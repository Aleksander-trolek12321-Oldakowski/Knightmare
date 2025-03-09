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
        SceneManager.LoadScene("Game");
    }
    public void QuitButton()
    {
        Application.Quit();
    }
}
