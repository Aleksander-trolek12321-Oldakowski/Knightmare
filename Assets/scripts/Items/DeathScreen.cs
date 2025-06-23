using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class DeathScreen : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void LoadMainMenu()
    {
        GameData.Instance.ResetData();

        string savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(savePath))
            File.Delete(savePath);
        SceneManager.LoadScene("MainMenu");
    }
}
