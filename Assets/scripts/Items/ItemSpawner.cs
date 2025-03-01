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
    [SerializeField] private TMP_Text itemPriceText;

    void Start()
    {
        GenerateRandomItem();
        if(itemPriceText != null)
        {
            itemPriceText.text=selectedItem.priceInShop +"";
        }
    }
    void Awake()
    {
        spawnedItems.Clear(); 
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
            if(selectedItem.priceInShop <= CoinManager.Instance.totalCoins && isInShop)
            {
                CoinManager.Instance.RemoveCoins(selectedItem.priceInShop);

                player.ApplyItemStats(selectedItem);

                if (selectedItem.itemSprite != null)
                {
                    InventoryUI.Instance.AddItemToUI(selectedItem.itemSprite);
                }

                Destroy(itemPriceText);
                Destroy(gameObject);
            }
            else if(isInShop ==false)
            {
                player.ApplyItemStats(selectedItem);

                if (selectedItem.itemSprite != null)
                {
                    InventoryUI.Instance.AddItemToUI(selectedItem.itemSprite);
                }

                if (treasureRoom != null)
                {

                    treasureRoom.DestroyItem(gameObject);
                }
                Destroy(gameObject);
            }
        }
    
   
        
    }

    public void ApplyItemData()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && selectedItem != null)
        {
            spriteRenderer.sprite = selectedItem.itemSprite;
        }
    }
}
