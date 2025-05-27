using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Toggle fullScreenToggle;
    public Slider sensitivitySlider;
    public TMP_Dropdown resolutionDropdown;

    // dekoracyjne obrazy – wyłączamy im RaycastTarget, żeby nie blokowały kliknięć
    public RawImage backgroundImage;
    public RawImage[] scalableImages;

    private bool isFullScreen = true;
    private float sensitivity = 5f;
    private Resolution[] availableResolutions;

    private const string PREF_FULLSCREEN      = "Fullscreen";
    private const string PREF_SENSITIVITY    = "Sensitivity";
    private const string PREF_RESOLUTION_IDX = "ResolutionIndex";

    void Start()
    {
        // 1. Wyłącz raycast target na dekoracjach
        if (backgroundImage != null) 
            backgroundImage.raycastTarget = false;
        if (scalableImages != null)
            foreach (var img in scalableImages)
                if (img != null) 
                    img.raycastTarget = false;

        // 2. Wczytaj dostępne rozdzielczości i wypełnij dropdown
        availableResolutions = Screen.resolutions;
        var options = new List<string>(availableResolutions.Length);
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var r = availableResolutions[i];
            options.Add($"{r.width} × {r.height}");
        }
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);

        // 3. Wczytaj zapisane ustawienia i zastosuj je od razu
        LoadSettings();

        // 4. Ustaw UI bez generowania callbacków przy starcie
        fullScreenToggle.SetIsOnWithoutNotify(isFullScreen);
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        resolutionDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt(PREF_RESOLUTION_IDX, 0));
    }

    // Metody publiczne – przypniesz je w Inspectorze do OnValueChanged
    public void OnFullScreenToggle(bool isOn)
    {
        isFullScreen      = isOn;
        Screen.fullScreen = isFullScreen;
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnSensitivityChanged(float value)
    {
        sensitivity = value;
        PlayerPrefs.SetFloat(PREF_SENSITIVITY, sensitivity);
        PlayerPrefs.Save();
    }

    public void OnResolutionChanged(int idx)
    {
        if (idx < 0 || idx >= availableResolutions.Length) return;

        // Zastosuj nową rozdzielczość od razu
        var r = availableResolutions[idx];
        Screen.SetResolution(r.width, r.height, isFullScreen);

        // Zapisz wybraną opcję
        PlayerPrefs.SetInt(PREF_RESOLUTION_IDX, idx);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        isFullScreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;
        sensitivity  = PlayerPrefs.GetFloat(PREF_SENSITIVITY, 5f);
        int idx      = PlayerPrefs.GetInt(PREF_RESOLUTION_IDX, 0);

        // Wstępnie zastosuj ustawienia ekranu
        Screen.fullScreen = isFullScreen;
        if (availableResolutions.Length > 0)
        {
            idx = Mathf.Clamp(idx, 0, availableResolutions.Length - 1);
            var r = availableResolutions[idx];
            Screen.SetResolution(r.width, r.height, isFullScreen);
        }
    }

    private void OnApplicationQuit()
    {
        // Na wszelki wypadek – jeszcze raz zapisz zmiany
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullScreen ? 1 : 0);
        PlayerPrefs.SetFloat(PREF_SENSITIVITY, sensitivity);
        PlayerPrefs.SetInt(PREF_RESOLUTION_IDX, resolutionDropdown.value);
        PlayerPrefs.Save();
    }
}
