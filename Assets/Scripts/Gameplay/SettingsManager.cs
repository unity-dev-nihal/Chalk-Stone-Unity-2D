using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Menu Panel")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider ambienceSlider;

    [Header("Toggles")]
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Toggle chalkAnimationToggle;

    private const string VibrationKey =
        "Settings_Vibration";

    private const string ChalkAnimationKey =
        "Settings_ChalkAnimation";

    public static bool VibrationEnabled { get; private set; }

    public static bool ChalkAnimationEnabled { get; private set; }

    private void Awake()
    {
        LoadSettings();
    }

    private void Start()
    {
        FindReferences();

        SetupSliders();
        SetupToggles();

        RegisterListeners();

        Debug.Log(
            "SettingsManager initialized."
        );
    }

    // =========================================================
    // FIND REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (settingsPanel == null)
        {
            settingsPanel = gameObject;
        }
    }

    // =========================================================
    // SETUP SLIDERS
    // =========================================================

    private void SetupSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;

            if (AudioManager.Instance != null)
            {
                musicSlider.SetValueWithoutNotify(
                    AudioManager.Instance.GetMusicVolume()
                );
            }
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;

            if (AudioManager.Instance != null)
            {
                sfxSlider.SetValueWithoutNotify(
                    AudioManager.Instance.GetSFXVolume()
                );
            }
        }

        if (ambienceSlider != null)
        {
            ambienceSlider.minValue = 0f;
            ambienceSlider.maxValue = 1f;

            if (AudioManager.Instance != null)
            {
                ambienceSlider.SetValueWithoutNotify(
                    AudioManager.Instance.GetAmbienceVolume()
                );
            }
        }
    }

    // =========================================================
    // SETUP TOGGLES
    // =========================================================

    private void SetupToggles()
    {
        if (vibrationToggle != null)
        {
            vibrationToggle.SetIsOnWithoutNotify(
                VibrationEnabled
            );
        }

        if (chalkAnimationToggle != null)
        {
            chalkAnimationToggle.SetIsOnWithoutNotify(
                ChalkAnimationEnabled
            );
        }
    }

    // =========================================================
    // REGISTER LISTENERS
    // =========================================================

    private void RegisterListeners()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(
                OnMusicSliderChanged
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(
                OnSFXSliderChanged
            );
        }

        if (ambienceSlider != null)
        {
            ambienceSlider.onValueChanged.AddListener(
                OnAmbienceSliderChanged
            );
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.AddListener(
                OnVibrationChanged
            );
        }

        if (chalkAnimationToggle != null)
        {
            chalkAnimationToggle.onValueChanged.AddListener(
                OnChalkAnimationChanged
            );
        }
    }

    // =========================================================
    // REMOVE LISTENERS
    // =========================================================

    private void OnDestroy()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(
                OnMusicSliderChanged
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(
                OnSFXSliderChanged
            );
        }

        if (ambienceSlider != null)
        {
            ambienceSlider.onValueChanged.RemoveListener(
                OnAmbienceSliderChanged
            );
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.RemoveListener(
                OnVibrationChanged
            );
        }

        if (chalkAnimationToggle != null)
        {
            chalkAnimationToggle.onValueChanged.RemoveListener(
                OnChalkAnimationChanged
            );
        }
    }

    // =========================================================
    // MUSIC
    // =========================================================

    private void OnMusicSliderChanged(
        float value)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMusicVolume(
            value
        );
    }

    // =========================================================
    // SFX
    // =========================================================

    private void OnSFXSliderChanged(
        float value)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetSFXVolume(
            value
        );
    }

    // =========================================================
    // AMBIENCE
    // =========================================================

    private void OnAmbienceSliderChanged(
        float value)
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetAmbienceVolume(
            value
        );
    }

    // =========================================================
    // VIBRATION
    // =========================================================

    private void OnVibrationChanged(
        bool enabled)
    {
        VibrationEnabled =
            enabled;

        PlayerPrefs.SetInt(
            VibrationKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Vibration: " +
            (enabled ? "ON" : "OFF")
        );
    }

    // =========================================================
    // CHALK ANIMATION
    // =========================================================

    private void OnChalkAnimationChanged(
        bool enabled)
    {
        ChalkAnimationEnabled =
            enabled;

        PlayerPrefs.SetInt(
            ChalkAnimationKey,
            enabled ? 1 : 0
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Chalk Animation: " +
            (enabled ? "ON" : "OFF")
        );
    }

    // =========================================================
    // LOAD SETTINGS
    // =========================================================

    private void LoadSettings()
    {
        VibrationEnabled =
            PlayerPrefs.GetInt(
                VibrationKey,
                1
            ) == 1;

        ChalkAnimationEnabled =
            PlayerPrefs.GetInt(
                ChalkAnimationKey,
                1
            ) == 1;
    }

    // =========================================================
    // BACK BUTTON
    // =========================================================

    public void Back()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        Debug.Log(
            "Returned to Main Menu."
        );
    }
}