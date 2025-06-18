using System;
using System.IO;
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

    // Player stats
    public float playerDamage;
    public float playerSpeed;
    public float playerAttackSpeed;
    public float playerRange;
    public float playerHealth;
    public int playerMaxHearts;

    // Abilities
    public bool canPoison;
    public bool canFire;
    public bool canSlow;
    public bool hasThorns;

    // Equipped item
    public ItemData currentEquippedItem;

    // Inventory icons
    public List<Sprite> collectedItemIcons = new List<Sprite>();

    // Scene transition data
    public string previousSceneName;
    public Vector3 returnPosition;

    // World state lists
    public List<string> destroyedPortals = new List<string>();
    public List<string> destroyedSpawnerIDs = new List<string>();
    public List<string> killedEnemies = new List<string>();

    // Dynamic enemy states
    public List<EnemyState> enemyStates = new List<EnemyState>();

    public int playerCoins;
    public GameObject portalPrefab;

    private string savePath;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");

        if (File.Exists(savePath))
        {
            LoadFromDisk();
        }
        else
        {
            int portalChance = UnityEngine.Random.Range(0, 5);
            if (portalPrefab != null)
                portalPrefab.SetActive(portalChance < 1);
        }
    }

    // Backwards compatibility alias
    public void SaveSceneName(Player player)
    {
        SaveSceneData(player);
    }

    public void SavePlayerData(Player player)
    {
        if (CoinManager.Instance != null)
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

        currentEquippedItem = player.GetItem();
    }

    public void SaveSceneData(Player player)
    {
        previousSceneName = SceneManager.GetActiveScene().name;
        if (player != null)
            returnPosition = player.transform.position;
    }

    public void CollectDynamicEnemyStates()
    {
        enemyStates.Clear();
        foreach (var en in FindObjectsOfType<enemy>())
        {
            string id = en.uniqueID;
            if (!killedEnemies.Contains(id))
            {
                enemyStates.Add(new EnemyState
                {
                    uniqueID = id,
                    position = new SerializableVector3(en.transform.position)
                });
            }
        }
    }

    public void SaveToDisk()
    {
        //Debug.Log($"[GameData] Saving {enemyStates.Count} enemyStates.");
        //CollectDynamicEnemyStates();

        var state = new GameDataState
        {
            playerDamage = playerDamage,
            playerSpeed = playerSpeed,
            playerAttackSpeed = playerAttackSpeed,
            playerRange = playerRange,
            playerHealth = playerHealth,
            playerMaxHearts = playerMaxHearts,
            canPoison = canPoison,
            canFire = canFire,
            canSlow = canSlow,
            hasThorns = hasThorns,
            collectedItemNames = currentEquippedItem,
            previousSceneName = previousSceneName,
            returnPosition = new SerializableVector3(returnPosition),
            destroyedPortals = destroyedPortals,
            destroyedSpawnerIDs = destroyedSpawnerIDs,
            killedEnemies = killedEnemies,
            enemyStates = enemyStates,
            playerCoins = playerCoins
        };

        try
        {
            string json = JsonUtility.ToJson(state, true);
            File.WriteAllText(savePath, json);
            Debug.Log("Game saved to: " + savePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Save Failed: " + ex.Message);
        }
    }

    public void LoadFromDisk()
    {
        try
        {
            string json = File.ReadAllText(savePath);
            var state = JsonUtility.FromJson<GameDataState>(json);

            playerDamage = state.playerDamage;
            playerSpeed = state.playerSpeed;
            playerAttackSpeed = state.playerAttackSpeed;
            playerRange = state.playerRange;
            playerHealth = state.playerHealth;
            playerMaxHearts = state.playerMaxHearts;

            canPoison = state.canPoison;
            canFire = state.canFire;
            canSlow = state.canSlow;
            hasThorns = state.hasThorns;

            currentEquippedItem = state.collectedItemNames;

            previousSceneName = state.previousSceneName;
            returnPosition = state.returnPosition.ToVector3();

            destroyedPortals = state.destroyedPortals ?? new List<string>();
            destroyedSpawnerIDs = state.destroyedSpawnerIDs ?? new List<string>();
            killedEnemies = state.killedEnemies ?? new List<string>();
            enemyStates = state.enemyStates ?? new List<EnemyState>();
            Debug.Log($"[GameData] Loaded {enemyStates.Count} enemyStates.");

            playerCoins = state.playerCoins;
            Debug.Log("Game loaded from: " + savePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading save: " + ex.Message);
        }
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
        playerDamage = playerSpeed = playerAttackSpeed = playerRange = playerHealth = 0f;
        playerMaxHearts = 0;
        canPoison = false;
        canFire = false;
        canSlow = false;
        hasThorns = false;
        changeCameraSize = false;
        currentEquippedItem = null;
        collectedItemIcons.Clear();
        previousSceneName = string.Empty;
        returnPosition = Vector3.zero;
        destroyedPortals.Clear();
        destroyedSpawnerIDs.Clear();
        killedEnemies.Clear();
        enemyStates.Clear();
        playerCoins = 0;
        if (File.Exists(savePath))
            File.Delete(savePath);
    }
}