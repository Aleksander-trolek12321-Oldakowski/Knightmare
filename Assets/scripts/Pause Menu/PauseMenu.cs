using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    private bool isPaused = false;
    public Player player;
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = new PlayerInput();
        playerInput.UI.Pause.performed += ctx => TogglePause();

    }

    void OnEnable()
    {


        playerInput.UI.Enable();
    }

    void OnDisable()
    {
      
        playerInput.UI.Disable();
    }

    private void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void ResumeGame()
    {
        AudioManager.Instance.PlaySound("MusicGame");
        AudioManager.Instance.StopSound("MusicPause");
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        player.playerInputActions.Player.Enable();
    }

    void PauseGame()
    {
        AudioManager.Instance.StopSound("MusicGame");
        AudioManager.Instance.PlaySound("MusicPause");
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        player.playerInputActions.Player.Disable();
    }

    public void LoadMainMenu()
    {
        AudioManager.Instance.StopSound("MusicPause");
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenu");
    }
}
