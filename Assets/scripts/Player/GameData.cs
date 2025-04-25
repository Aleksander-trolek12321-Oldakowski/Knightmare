using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    public float playerDamage;
    public float playerSpeed;
    public float playerAttackSpeed;
    public float playerRange;
    public float playerHealth;
    public int playerMaxHearts;

    public bool canPoison;
    public bool canFire;
    public bool canSlow;
    public bool hasThorns;

    public List<Sprite> collectedItemIcons = new List<Sprite>();
    public string previousSceneName;
    public Vector3 returnPosition;

    public List<string> destroyedPortals = new List<string>();

    public int playerCoins ;
    public List<string> killedEnemies = new List<string>();



    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SavePlayerData(Player player)
    {
        playerCoins = CoinManager.Instance.totalCoins;

        playerDamage = player.GetDamage();
        playerSpeed = player.GetSpeed();
        playerAttackSpeed = player.GetAttackSpeed();
        playerRange = player.GetRange();
        playerHealth = player.GetCurrentHealth();
        playerMaxHearts = player.GetMaxHearts();

        canFire = player.GetCanFire();
        canPoison = player.GetCanPoison();
        canSlow = player.GetCanSlow();
        hasThorns = player.GetHasThorns();


    }
    public void SaveSceneName(Player player)
    {
        previousSceneName = SceneManager.GetActiveScene().name;
        returnPosition = player.transform.position;
    }

    public void LoadPlayerData(Player player)
    {
        player.ApplyLoadedStats(
            playerDamage, playerSpeed, playerAttackSpeed,
            playerRange, playerHealth, playerMaxHearts,
            canPoison, canFire, canSlow, hasThorns
        );
    }
    public void ResetData()
    {
        playerDamage = 0;
        playerSpeed = 0;
        playerAttackSpeed = 0;
        playerRange = 0;
        playerHealth = 0;
        playerMaxHearts = 0;

        canPoison = false;
        canFire = false;
        canSlow = false;
        hasThorns = false;

        collectedItemIcons.Clear();
        previousSceneName = "";
        returnPosition = Vector3.zero;
        destroyedPortals.Clear();

        playerCoins = 0;
        killedEnemies.Clear();
    }

}
