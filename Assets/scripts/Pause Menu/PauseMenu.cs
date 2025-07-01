using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public Player player;  // odwołanie do komponentu Player w scenie

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    private void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();

        Debug.Log("TogglePause – isPaused = " + isPaused);
    }

    public void ResumeGame()
    {
        // Odtwórz muzykę i UI
        AudioManager.Instance.StopSound("MusicPause");
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f;
        isPaused = false;
        AudioManager.Instance.PlayPlaylist("MusicGame1", "MusicGame2");
        player.playerInputActions.Player.Enable();
        player.playerInputActions.UI.Disable();
    }

    void PauseGame()
    {
        AudioManager.Instance.StopPlaylist();
        AudioManager.Instance.StopSound("MusicSpecialRoom");
        AudioManager.Instance.PlaySound("MusicPause");
        if(pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);

        }
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
        isPaused = true;
        player.playerInputActions.Player.Disable();
        player.playerInputActions.UI.Enable();

        // Zresetuj zaznaczenie w UI
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void LoadMainMenu()
    {
        // Przed wyjściem do menu upewnij się, że mamy zapisany stan
        GameData.Instance.SaveSceneData(player);
        GameData.Instance.SavePlayerData(player);
        GameData.Instance.SaveToDisk();

        AudioManager.Instance.StopSound("MusicPause");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}