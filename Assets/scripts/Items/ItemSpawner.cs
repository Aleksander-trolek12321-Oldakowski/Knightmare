using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

public class ItemSpawner : MonoBehaviour
{
    public ItemData[] possibleItems;
    private static HashSet<ItemData> spawnedItems = new HashSet<ItemData>();
    private ItemData selectedItem;
    public bool isInShop = false;
    public TreasureRoom treasureRoom;
    public SecretRoom secretRoom;
    [SerializeField] private TMP_Text itemPriceText;
    [SerializeField] private GameObject questionMarkPrefab;
    public string spawnerID;

    private ScenePersistence persist;

    void Awake()
    {
        persist = GetComponent<ScenePersistence>();
        persist.isSpawner = true;
        persist.uniqueID = spawnerID;
        spawnedItems.Clear();
        Debug.Log($"[ItemSpawner] Awake: gameObject.name = '{gameObject.name}', persist.uniqueID = '{persist.uniqueID}'");
    }

    void Start()
    {
        GenerateRandomItem();
        if (itemPriceText != null)
        {
            itemPriceText.text = selectedItem.priceInShop + "";
        }
    }

    private void GenerateRandomItem()
    {
        List<ItemData> availableItems = new List<ItemData>();
        foreach (ItemData item in possibleItems)
        {
            if (!spawnedItems.Contains(item))
            {
                availableItems.Add(item);
            }
        }

        if (availableItems.Count > 0)
        {
            int randomIndex = Random.Range(0, availableItems.Count);
            selectedItem = availableItems[randomIndex];
            spawnedItems.Add(selectedItem);

            ApplyItemData();
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            if (selectedItem.priceInShop <= CoinManager.Instance.totalCoins && isInShop)
            {
                CoinManager.Instance.RemoveCoins(selectedItem.priceInShop);

                player.ApplyItemStats(selectedItem);

                if (selectedItem.itemSprite != null)
                {
                    InventoryUI.Instance.AddItemToUI(selectedItem.itemSprite, selectedItem.name);
                    GameData.Instance.collectedItemIcons.Add(selectedItem.itemSprite);
                    GameData.Instance.collectedItems.Add(selectedItem);
                }

                persist.RegisterRemoval();
                GameData.Instance.SaveToDisk();
                Destroy(itemPriceText);
                Destroy(gameObject);
            }
            else if (isInShop == false)
            {
                player.ApplyItemStats(selectedItem);

                if (selectedItem.itemSprite != null)
                {
                    InventoryUI.Instance.AddItemToUI(selectedItem.itemSprite, selectedItem.name);
                    GameData.Instance.collectedItemIcons.Add(selectedItem.itemSprite);
                    GameData.Instance.collectedItems.Add(selectedItem);
                }


                if (treasureRoom != null)
                {
                    persist.RegisterRemoval();
                    GameData.Instance.SaveToDisk();
                    treasureRoom.DestroyItem(gameObject);
                }
                if (secretRoom != null)
                {
                    persist.RegisterRemoval();
                    GameData.Instance.SaveToDisk();
                    secretRoom.DestroyItem(gameObject);
                }
                persist.RegisterRemoval();
                GameData.Instance.SaveToDisk();
                Destroy(gameObject);
            }
        }



    }

    public void ApplyItemData()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && selectedItem != null && secretRoom == null)
        {
            spriteRenderer.sprite = selectedItem.itemSprite;
        }
        else
        {
            GameObject questionMark = Instantiate(questionMarkPrefab, transform.position, Quaternion.identity, transform);
            questionMark.transform.localPosition = Vector3.zero;
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }
    }
}
