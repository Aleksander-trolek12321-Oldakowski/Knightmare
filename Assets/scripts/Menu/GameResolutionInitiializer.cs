using UnityEngine;

public class GameResolutionInitializer : MonoBehaviour
{
    void Awake()
    {
        // Odczytaj zapisane ustawienia
        int idx = PlayerPrefs.GetInt("ResolutionIndex", 0);
        bool fs = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        Resolution[] res = Screen.resolutions;
        if (res != null && idx >= 0 && idx < res.Length)
        {
            var r = res[idx];
            Screen.SetResolution(r.width, r.height, fs);
        }
        // Jeśli coś jest nie tak z idx, zostanie użyta obecna rozdzielczość ekranu
    }
}