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

    [Header("Audio References")]
    public Slider musicVolumeSlider;   // ← nowy
    public Slider sfxVolumeSlider;     // ← nowy

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
        // Wyłącz dekoracyjne raycasty
        if (backgroundImage) backgroundImage.raycastTarget = false;
        if (scalableImages != null)
            foreach (var img in scalableImages)
                if (img) img.raycastTarget = false;

        // Rozdzielczości
        availableResolutions = Screen.resolutions;
        var options = new List<string>(availableResolutions.Length);
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var r = availableResolutions[i];
            options.Add($"{r.width} × {r.height}");
        }
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);

        LoadSettings();

        fullScreenToggle.SetIsOnWithoutNotify(isFullScreen);
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        resolutionDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt(PREF_RESOLUTION_IDX, 0));

        if (musicVolumeSlider != null)
        {
            float mv = PlayerPrefs.GetFloat(AudioManager.PREF_MUSIC_VOL, 1f);
            musicVolumeSlider.SetValueWithoutNotify(mv);
        }
        if (sfxVolumeSlider != null)
        {
            float sv = PlayerPrefs.GetFloat(AudioManager.PREF_SFX_VOL, 1f);
            sfxVolumeSlider.SetValueWithoutNotify(sv);
        }
    }

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
        var r = availableResolutions[idx];
        Screen.SetResolution(r.width, r.height, isFullScreen);
        PlayerPrefs.SetInt(PREF_RESOLUTION_IDX, idx);
        PlayerPrefs.Save();
    }

    public void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    private void LoadSettings()
    {
        isFullScreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;
        sensitivity  = PlayerPrefs.GetFloat(PREF_SENSITIVITY, 5f);
        int idx      = PlayerPrefs.GetInt(PREF_RESOLUTION_IDX, 0);

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
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullScreen ? 1 : 0);
        PlayerPrefs.SetFloat(PREF_SENSITIVITY, sensitivity);
        PlayerPrefs.SetInt(PREF_RESOLUTION_IDX, resolutionDropdown.value);
        PlayerPrefs.Save();
    }
}