using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using enemySpace;  // import namespace for enemy class

[Serializable]
public class EnemyState
{
    public string uniqueID;
    public SerializableVector3 position;
    public int prefabIndex;
}

[Serializable]
public class GameDataState
{
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
    public bool changeCameraSize;

    public string currentItemName;
    public ItemData collectedItemNames;

    public string previousSceneName;
    public SerializableVector3 returnPosition;

    public List<string> destroyedPortals;
    public List<string> destroyedSpawnerIDs;
    public List<string> killedEnemies;

    public List<EnemyState> enemyStates;
    public int playerCoins;
}

[Serializable]
public struct SerializableVector3
{
    public float x, y, z;
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

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
    public ItemData currentEquippedItem;

    public List<Sprite> collectedItemIcons = new List<Sprite>();
    public string previousSceneName;
    public Vector3 returnPosition;

    public List<string> destroyedPortals = new List<string>();

    public int playerCoins ;
    public List<string> killedEnemies = new List<string>();

    public GameObject portalPrefab;



    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        int portalChance = Random.Range(0, 5);
        if(portalChance < 1)
        {
            portalPrefab.SetActive(true);
        }
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
        hasThorns = player.GetHasThorns();
        changeCameraSize = player.GetCamerSize();
        Debug.Log(changeCameraSize + " " + player.GetCamerSize());

        hasThorns = player.GetHasThorns();
        currentEquippedItem = player.GetItem();

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
            canPoison, canFire, canSlow, hasThorns, currentEquippedItem, changeCameraSize
        );
        if (CoinManager.Instance != null)
            CoinManager.Instance.totalCoins = playerCoins;
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
        changeCameraSize = false;
        currentEquippedItem = null;
        collectedItemIcons.Clear();
        previousSceneName = "";
        returnPosition = Vector3.zero;
        destroyedPortals.Clear();
        currentEquippedItem = null;
        playerCoins = 0;
        killedEnemies.Clear();
    }

}