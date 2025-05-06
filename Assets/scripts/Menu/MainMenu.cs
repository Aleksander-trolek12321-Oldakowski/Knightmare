using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
     void Start()
    {
        AudioManager.Instance.PlaySound("MusicMenu");
    }
    public void PlayButton()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.ResetData();
        }
        SceneManager.LoadScene("Game");
    }
    public void QuitButton()
    {
        Application.Quit();
    }
}
