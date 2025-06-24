using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BloodRoom : MonoBehaviour
{
    public ItemData[] possibleItems;
    private static HashSet<ItemData> spawnedItems = new HashSet<ItemData>();
    private ItemData selectedItem;
    [SerializeField] private TMP_Text itemHealthCostText;
    [SerializeField] private GameObject healthIcon;
    [SerializeField] private SpawnPortal spawnPortal;

    void Start()
    {
        GenerateRandomItem();

        if (itemHealthCostText != null)
        {
            itemHealthCostText.text = "" + selectedItem.priceInBloodRoom;
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

            if (player != null )
            {
                spawnPortal.CreateReturnPortal();
                player.ApplyItemStats(selectedItem);
                player.TakeDamage(selectedItem.priceInBloodRoom * 4, transform.position);

                if (selectedItem.itemSprite != null)
                {
                    InventoryUI.Instance.AddItemToUI(selectedItem.itemSprite, selectedItem.name);
                    GameData.Instance.collectedItemIcons.Add(selectedItem.itemSprite);
                }

               

                Destroy(gameObject);
                Destroy(itemHealthCostText);
                Destroy(healthIcon);
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
