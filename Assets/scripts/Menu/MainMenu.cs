using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Buttons")]    
    public Button newGameButton;
    public Button continueButton;
    public Button quitButton;

    private string savePath;

    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    void Start()
    {
        AudioManager.Instance.PlaySound("MusicMenu");

        // Configure buttons
        SetupNewGameButton();
        SetupContinueButton();
        SetupQuitButton();
    }

    private void SetupNewGameButton()
    {
        newGameButton.onClick.RemoveAllListeners();
        newGameButton.onClick.AddListener(OnNewGame);
    }

    private void SetupContinueButton()
    {
        // Gray out if no save exists
        bool hasSave = File.Exists(savePath);
        var colors = continueButton.colors;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        continueButton.colors = colors;
        continueButton.interactable = hasSave;

        continueButton.onClick.RemoveAllListeners();
        if (hasSave)
            continueButton.onClick.AddListener(OnContinue);
    }

    private void SetupQuitButton()
    {
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(OnQuit);
    }

    private void OnNewGame()
    {
        // Delete existing save and reset runtime data
        if (File.Exists(savePath))
            File.Delete(savePath);
        if (GameData.Instance != null)
            GameData.Instance.ResetData();

        Time.timeScale = 1f; 

        SceneManager.LoadScene("Game");
    }

    private void OnContinue()
    {
        // Load saved data from disk
        if (GameData.Instance != null)
            GameData.Instance.LoadFromDisk();

        // Load saved scene
        string scene = GameData.Instance.previousSceneName;
        Time.timeScale = 1f; 
        SceneManager.LoadScene(scene);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Restore player position & stats
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = GameData.Instance.returnPosition;
            var player = playerObj.GetComponent<Player>();
            GameData.Instance.LoadPlayerData(player);
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnQuit()
    {
        Application.Quit();
    }
}
