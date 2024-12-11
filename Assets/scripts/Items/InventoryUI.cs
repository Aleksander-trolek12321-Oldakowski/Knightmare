using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] private GameObject itemIconPrefab;
    [SerializeField] private Transform inventoryPanel; 

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
    }
}