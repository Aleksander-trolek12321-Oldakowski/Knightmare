using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] private GameObject itemIconPrefab;
    [SerializeField] private Transform inventoryPanel;

    private List<GameObject> itemIcons = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItemToUI(Sprite itemSprite)
    {
        GameObject newIcon = Instantiate(itemIconPrefab, inventoryPanel);
        ItemIcon iconComponent = newIcon.GetComponent<ItemIcon>();
        iconComponent.SetIcon(itemSprite);
        itemIcons.Add(newIcon);
    }

    public void RemoveLastItemIcon()
    {
        if (itemIcons.Count > 0)
        {
            GameObject lastIcon = itemIcons[itemIcons.Count - 1];
            itemIcons.RemoveAt(itemIcons.Count - 1);
            Destroy(lastIcon);
        }
    }
}
