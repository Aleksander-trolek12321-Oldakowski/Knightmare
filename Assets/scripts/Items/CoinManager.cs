using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    public int totalCoins = 0;
    [SerializeField] private TMP_Text coinText; 

    private void Awake()
    {
        if (GameData.Instance != null)
        {
            totalCoins = GameData.Instance.playerCoins;
        }
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        coinText.text = "" + totalCoins;

    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        coinText.text = "" + totalCoins;
    }  
    public void RemoveCoins(int amount)
    {
        totalCoins -= amount;
        coinText.text = "" + totalCoins;
    }


}
