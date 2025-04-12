using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void LoadPlayerData(Player player)
    {
        player.ApplyLoadedStats(
            playerDamage, playerSpeed, playerAttackSpeed,
            playerRange, playerHealth, playerMaxHearts,
            canPoison, canFire, canSlow, hasThorns
        );
    }
}
