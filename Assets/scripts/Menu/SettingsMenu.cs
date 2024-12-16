using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    public Toggle fullScreenToggle; // Using Toggle for fullscreen option
    public Slider sensitivitySlider;
    public TMP_Dropdown resolutionDropdown;
    public RawImage[] rawImages; // Array to hold multiple RawImages

    private bool isFullScreen = true;
    private float sensitivity = 5.0f;
    private Resolution[] resolutions;

    void Start()
    {
        // Log all available resolutions
        LogAvailableResolutions();

        // Get available resolutions
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
        }

        resolutionDropdown.AddOptions(options);

        // Load saved settings
        LoadSettings();
        
        // Initialize Toggle and Slider values
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
            PlayerPrefs.SetInt("ResolutionIndex", index); // Save the selected index
            PlayerPrefs.Save(); // Ensure it's saved

            // Adjust all RawImages scale based on resolution
            AdjustRawImageScale(selectedResolution.width, selectedResolution.height);
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
        // Load settings from PlayerPrefs or set to default if not set
        isFullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        sensitivity = PlayerPrefs.GetFloat("Sensitivity", 5.0f);
        int resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);

        // Check if the loaded resolution index is valid
        if (resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            Screen.SetResolution(resolutions[resolutionIndex].width, resolutions[resolutionIndex].height, isFullScreen);
            resolutionDropdown.value = resolutionIndex; // Set dropdown value to match saved index

            // Adjust all RawImages scale based on loaded resolution
            AdjustRawImageScale(resolutions[resolutionIndex].width, resolutions[resolutionIndex].height);
        }
        else
        {
            Debug.LogWarning("Loaded resolution index is out of range, setting to default.");
            Screen.SetResolution(resolutions[0].width, resolutions[0].height, isFullScreen); // Set to the first resolution as fallback
            resolutionDropdown.value = 0; // Reset dropdown to the first option

            // Adjust all RawImages scale based on default resolution
            AdjustRawImageScale(resolutions[0].width, resolutions[0].height);
        }

        Debug.Log("Loaded Settings: Fullscreen - " + isFullScreen + ", Sensitivity - " + sensitivity + ", Resolution Index - " + resolutionIndex);
    }

    private void OnApplicationQuit()
    {
        // Save settings when the application quits
        SaveSettings();
    }

    private void LogAvailableResolutions()
    {
        Debug.Log("Available Resolutions:");
        foreach (var resolution in Screen.resolutions)
        {
            Debug.Log(resolution.width + " x " + resolution.height);
        }
    }

    // Adjusts the scale for all RawImages based on the selected resolution
    private void AdjustRawImageScale(int width, int height)
    {
        if (rawImages != null && rawImages.Length > 0)
        {
            // Calculate scaling factor based on resolution
            float scaleX = (float)width / 1920f; // Assuming 1920x1080 as base resolution
            float scaleY = (float)height / 1080f;

            foreach (var rawImage in rawImages)
            {
                rawImage.rectTransform.localScale = new Vector3(scaleX, scaleY, 1);
            }
            Debug.Log("All RawImages scale adjusted: X = " + scaleX + ", Y = " + scaleY);
        }
    }
}
