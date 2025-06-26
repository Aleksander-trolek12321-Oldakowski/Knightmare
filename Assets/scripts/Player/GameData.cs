using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using enemySpace;

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
    public bool changeCameraSize;

    // Equipped item
    public string currentItemName;
    public List<string> collectedItemIDs;

    // Scene transition data
    public string previousSceneName;
    public SerializableVector3 returnPosition;

    // World state
    public List<string> destroyedPortals;
    public List<string> destroyedSpawnerIDs;
    public List<string> killedEnemies;

    // Dynamic enemies
    public List<EnemyState> enemyStates;

    // Kill counters
    public int boomlingKills;
    public int skeletonMeleeKills;
    public int zombieKills;

    // Player coins
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
    public bool changeCameraSize;

    // Inventory items
    public ItemData currentEquippedItem;
    public List<ItemData> collectedItems = new List<ItemData>();
    public List<Sprite> collectedItemIcons = new List<Sprite>();

    // Scene transition data
    public string previousSceneName;
    public Vector3 returnPosition;

    // World state
    public List<string> destroyedPortals = new List<string>();
    public List<string> destroyedSpawnerIDs = new List<string>();
    public List<string> killedEnemies = new List<string>();

    // Dynamic enemies
    public List<EnemyState> enemyStates = new List<EnemyState>();

    // Kill counters
    public int boomlingKills;
    public int skeletonMeleeKills;
    public int zombieKills;

    // Player coins
    public int playerCoins;
    public GameObject portalPrefab;

    private string savePath;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var portalComp = FindFirstObjectByType<Portal>();
        if (portalComp != null)
        portalPrefab = portalComp.gameObject;

        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(savePath)) LoadFromDisk();

        int portalChance = UnityEngine.Random.Range(0, 2);
        if (portalChance < 1)
        {
            portalPrefab.SetActive(true);
            portalComp.gameObject.SetActive(true);  
        }
    }

    public void SaveSceneName(Player player)
    {
        SaveSceneData(player);
    }

    public void SavePlayerData(Player player)
    {
        // Stats
        playerCoins = CoinManager.Instance != null ? CoinManager.Instance.totalCoins : playerCoins;
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
        changeCameraSize = player.GetCamerSize();
        currentEquippedItem = player.GetItem();
    }

    public void SaveSceneData(Player player)
    {
        previousSceneName = SceneManager.GetActiveScene().name;
        if (player!=null) returnPosition = player.transform.position;
    }

    public void SaveKillCounters()
    {
        boomlingKills = Boomling.boomlingKillCounter;
        skeletonMeleeKills = Skeleton_Meele.skeletonKillCounter;
        zombieKills = Zombie.zombieKillCounter;
    }

    public void SaveToDisk()
    {
        // Before saving
        SaveKillCounters();

        var state = new GameDataState {
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
            currentItemName = currentEquippedItem!=null?currentEquippedItem.name:string.Empty,
            collectedItemIDs = collectedItems.ConvertAll(i=>i.name),
            previousSceneName = previousSceneName,
            returnPosition = new SerializableVector3(returnPosition),
            destroyedPortals = destroyedPortals,
            destroyedSpawnerIDs = destroyedSpawnerIDs,
            killedEnemies = killedEnemies,
            enemyStates = enemyStates,
            boomlingKills = boomlingKills,
            skeletonMeleeKills = skeletonMeleeKills,
            zombieKills = zombieKills,
            playerCoins = playerCoins
        };
        var json = JsonUtility.ToJson(state,true);
        File.WriteAllText(savePath,json);
    }

    public void LoadFromDisk()
    {
        var json = File.ReadAllText(savePath);
        var state = JsonUtility.FromJson<GameDataState>(json);
        // Stats
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
        currentEquippedItem = !string.IsNullOrEmpty(state.currentItemName)?
            Resources.Load<ItemData>($"Items/{state.currentItemName}"):null;
        if (state.collectedItemIDs != null)
        {
            foreach (var id in state.collectedItemIDs)
            {
                var item = Resources.Load<ItemData>($"Items/{id}");
                if (item != null)
                {
                    collectedItems.Add(item);
                    collectedItemIcons.Add(item.itemSprite);
                }
                else
                {
                    Debug.LogWarning($"ItemData '{id}' not found in Resources/Items.");
                }
            }
        }

        previousSceneName = state.previousSceneName;
        returnPosition = state.returnPosition.ToVector3();
        destroyedPortals = state.destroyedPortals;
        destroyedSpawnerIDs = state.destroyedSpawnerIDs;
        killedEnemies = state.killedEnemies;
        enemyStates = state.enemyStates;
        // Kill counters
        boomlingKills = state.boomlingKills;
        skeletonMeleeKills = state.skeletonMeleeKills;
        zombieKills = state.zombieKills;
        Boomling.boomlingKillCounter = boomlingKills;
        Skeleton_Meele.skeletonKillCounter = skeletonMeleeKills;
        Zombie.zombieKillCounter = zombieKills;
        playerCoins = state.playerCoins;
    }

    public void LoadPlayerData(Player player)
    {
        player.ApplyLoadedStats(
            playerDamage, playerSpeed, playerAttackSpeed,
            playerRange, playerHealth, playerMaxHearts,
            canPoison, canFire, canSlow, hasThorns, currentEquippedItem, changeCameraSize);
        if (CoinManager.Instance!=null) CoinManager.Instance.totalCoins = playerCoins;
    }

    public void ResetData()
    {
        playerDamage=playerSpeed=playerAttackSpeed=playerRange=playerHealth=0;
        playerMaxHearts=0;
        canPoison=canFire=canSlow=hasThorns=changeCameraSize=false;
        currentEquippedItem=null;
        collectedItems.Clear(); collectedItemIcons.Clear();
        previousSceneName=string.Empty;
        returnPosition=Vector3.zero;
        destroyedPortals.Clear(); destroyedSpawnerIDs.Clear(); killedEnemies.Clear();
        enemyStates.Clear();
        boomlingKills=skeletonMeleeKills=zombieKills=0;
        Boomling.boomlingKillCounter       = 0;
        Skeleton_Meele.skeletonKillCounter = 0;
        Zombie.zombieKillCounter           = 0;
        playerCoins =0;
        if(File.Exists(savePath)) File.Delete(savePath);
    }
}
