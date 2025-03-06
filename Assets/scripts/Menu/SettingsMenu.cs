using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    public Toggle fullScreenToggle;
    public Slider sensitivitySlider;
    public TMP_Dropdown resolutionDropdown;
    public RawImage backgroundImage; // Obraz tła
    public RawImage[] scalableImages; // Tablica obrazów do skalowania

    private bool isFullScreen = true;
    private float sensitivity = 5.0f;
    private Resolution[] resolutions;

    private const float baseWidth = 1920f; // Baza rozdzielczości dla skalowania
    private const float baseHeight = 1080f;

    void Start()
    {
        // Pobranie dostępnych rozdzielczości
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
        }

        resolutionDropdown.AddOptions(options);

        // Wczytanie ustawień
        LoadSettings();

        // Ustawienie początkowych wartości UI
        fullScreenToggle.isOn = isFullScreen;
        sensitivitySlider.value = sensitivity;
    }

    public void ToggleFullScreen(bool isOn)
    {
        isFullScreen = isOn;
        Screen.fullScreen = isFullScreen;
        Debug.Log("Fullscreen: " + isFullScreen);
    }

    public void SetSensitivity(float value)
    {
        sensitivity = value;
        Debug.Log("Sensitivity: " + sensitivity);
    }

    public void SetResolution(int index)
    {
        if (index >= 0 && index < resolutions.Length)
        {
            Resolution selectedResolution = resolutions[index];
            Screen.SetResolution(selectedResolution.width, selectedResolution.height, isFullScreen);
            Debug.Log("Resolution set to: " + selectedResolution.width + " x " + selectedResolution.height);
            PlayerPrefs.SetInt("ResolutionIndex", index);
            PlayerPrefs.Save();

            // Dostosowanie obrazów do nowej rozdzielczości
            AdjustImageScales(selectedResolution.width, selectedResolution.height);
        }
        else
        {
            Debug.LogWarning("Selected resolution index is out of range.");
        }
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("Fullscreen", isFullScreen ? 1 : 0);
        PlayerPrefs.SetFloat("Sensitivity", sensitivity);
        PlayerPrefs.Save();
        Debug.Log("Settings Saved: Fullscreen - " + isFullScreen + ", Sensitivity - " + sensitivity);
    }

    private void LoadSettings()
    {
        isFullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        sensitivity = PlayerPrefs.GetFloat("Sensitivity", 5.0f);
        int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);

        if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            Screen.SetResolution(resolutions[resolutionIndex].width, resolutions[resolutionIndex].height, isFullScreen);
            resolutionDropdown.value = resolutionIndex;

            AdjustImageScales(resolutions[resolutionIndex].width, resolutions[resolutionIndex].height);
        }
        else
        {
            Debug.LogWarning("Loaded resolution index is out of range, setting to default.");
            Screen.SetResolution(resolutions[0].width, resolutions[0].height, isFullScreen);
            resolutionDropdown.value = 0;

            AdjustImageScales(resolutions[0].width, resolutions[0].height);
        }

        Debug.Log("Loaded Settings: Fullscreen - " + isFullScreen + ", Sensitivity - " + sensitivity + ", Resolution Index - " + resolutionIndex);
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }

    // Skalowanie wszystkich obrazów
    private void AdjustImageScales(int width, int height)
    {
        float scaleX = width / baseWidth;
        float scaleY = height / baseHeight;

        // Skalowanie wszystkich obrazów w tablicy
        if (scalableImages != null && scalableImages.Length > 0)
        {
            foreach (var image in scalableImages)
            {
                if (image != null)
                {
                    image.rectTransform.localScale = new Vector3(scaleX, scaleY, 1);
                }
            }
            Debug.Log("All images scaled: X = " + scaleX + ", Y = " + scaleY);
        }

        // Skalowanie tła
        if (backgroundImage != null)
        {
            backgroundImage.rectTransform.localScale = new Vector3(scaleX, scaleY, 1);
            Debug.Log("Background scaled: X = " + scaleX + ", Y = " + scaleY);
        }
    }
}
