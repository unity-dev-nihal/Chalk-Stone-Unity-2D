using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class VibrationToggleController : MonoBehaviour
{
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    private void Start()
    {
        if (toggle == null)
            return;

        if (VibrationManager.Instance == null)
        {
            Debug.LogWarning(
                "VibrationToggleController: " +
                "VibrationManager instance was not found."
            );

            return;
        }

        // Load the saved vibration state into the toggle.
        toggle.SetIsOnWithoutNotify(
            VibrationManager.Instance.IsVibrationEnabled()
        );

        toggle.onValueChanged.AddListener(
            OnVibrationToggleChanged
        );

        Debug.Log(
            $"Vibration Toggle initialized → " +
            $"{(toggle.isOn ? "ON" : "OFF")}"
        );
    }

    private void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(
                OnVibrationToggleChanged
            );
        }
    }

    private void OnVibrationToggleChanged(bool enabled)
    {
        if (VibrationManager.Instance == null)
        {
            Debug.LogWarning(
                "VibrationToggleController: " +
                "VibrationManager instance is missing."
            );

            return;
        }

        VibrationManager.Instance.SetVibrationEnabled(
            enabled
        );

        Debug.Log(
            $"Vibration Toggle changed → " +
            $"{(enabled ? "ON" : "OFF")}"
        );
    }
}