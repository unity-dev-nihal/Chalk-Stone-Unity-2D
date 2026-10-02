using UnityEngine;

public class VibrationManager : MonoBehaviour
{
    public static VibrationManager Instance { get; private set; }

    private const string VibrationEnabledKey =
        "Settings_VibrationEnabled";

    [Header("Default Settings")]
    [SerializeField] private bool defaultVibrationEnabled = true;

    [Header("Haptic Settings")]
    [SerializeField] private int invalidMoveDuration = 50;
    [SerializeField] private int invalidMoveStrength = 110;

    [SerializeField] private int drawDetectedDuration = 100;
    [SerializeField] private int drawDetectedStrength = 210;

    [SerializeField] private int drawDuration = 130;
    [SerializeField] private int drawStrength = 255;

    [SerializeField] private int victoryDuration = 120;
    [SerializeField] private int victoryStrength = 230;

    public bool VibrationEnabled { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject vibrator;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        LoadSettings();

#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidVibrator();
#endif

        Debug.Log(
            $"VibrationManager initialized. " +
            $"Vibration Enabled: {VibrationEnabled}"
        );
    }

    private void LoadSettings()
    {
        int savedValue =
            PlayerPrefs.GetInt(
                VibrationEnabledKey,
                defaultVibrationEnabled ? 1 : 0
            );

        VibrationEnabled = savedValue == 1;
    }

    public void SetVibrationEnabled(bool enabled)
    {
        VibrationEnabled = enabled;

        PlayerPrefs.SetInt(
            VibrationEnabledKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"VIBRATION SETTING → " +
            $"{(enabled ? "ON" : "OFF")}"
        );
    }

    public bool IsVibrationEnabled()
    {
        return VibrationEnabled;
    }

    private bool CanVibrate()
    {
        int savedValue =
            PlayerPrefs.GetInt(
                VibrationEnabledKey,
                defaultVibrationEnabled ? 1 : 0
            );

        VibrationEnabled = savedValue == 1;

        return VibrationEnabled;
    }

#if UNITY_ANDROID && !UNITY_EDITOR

    private void InitializeAndroidVibrator()
    {
        try
        {
            using (AndroidJavaClass unityPlayer =
                   new AndroidJavaClass(
                       "com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>(
                        "currentActivity"
                    );

                vibrator =
                    activity.Call<AndroidJavaObject>(
                        "getSystemService",
                        "vibrator"
                    );
            }

            Debug.Log(
                "VibrationManager: Android vibrator initialized."
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "VibrationManager: Failed to initialize Android vibrator.\n" +
                exception.Message
            );
        }
    }

#endif

    private void Vibrate(int duration, int strength)
    {
        if (!CanVibrate())
        {
            Debug.Log(
                "Vibration blocked — setting is OFF."
            );

            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR

        if (vibrator == null)
        {
            InitializeAndroidVibrator();
        }

        if (vibrator == null)
        {
            Debug.LogWarning(
                "VibrationManager: Android vibrator unavailable."
            );

            return;
        }

        try
        {
            using (AndroidJavaClass vibrationEffectClass =
                   new AndroidJavaClass(
                       "android.os.VibrationEffect"))
            {
                AndroidJavaObject vibrationEffect =
                    vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        (long)duration,
                        Mathf.Clamp(strength, 1, 255)
                    );

                vibrator.Call(
                    "vibrate",
                    vibrationEffect
                );
            }

            Debug.Log(
                $"Haptic → Duration: {duration}ms | " +
                $"Strength: {strength}"
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "VibrationManager: Android haptic failed.\n" +
                exception.Message
            );
        }

#elif UNITY_IOS && !UNITY_EDITOR

        // iOS fallback.
        // Unity's Handheld.Vibrate does not expose
        // custom duration or amplitude control.
        Handheld.Vibrate();

#else

        Debug.Log(
            $"Haptic simulated → Duration: {duration}ms | " +
            $"Strength: {strength}"
        );

#endif
    }

    public void VibrateInvalidMove()
    {
        Vibrate(
            invalidMoveDuration,
            invalidMoveStrength
        );
    }

    public void VibrateDrawDetected()
    {
        Vibrate(
            drawDetectedDuration,
            drawDetectedStrength
        );
    }

    public void VibrateDraw()
    {
        Vibrate(
            drawDuration,
            drawStrength
        );
    }

    public void VibrateVictory()
    {
        Vibrate(
            victoryDuration,
            victoryStrength
        );
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        if (vibrator != null)
        {
            vibrator.Dispose();
            vibrator = null;
        }

#endif
    }
}