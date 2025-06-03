using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathScreenItems : MonoBehaviour
{

    [SerializeField] private GameObject itemIconPrefab;
    [SerializeField] private Transform inventoryPanel;

    private List<GameObject> itemIcons = new List<GameObject>();
    void Awake()
    {
        if (GameData.Instance != null)
        {

            foreach (Sprite icon in GameData.Instance.collectedItemIcons)
            {
               AddItemToUI(icon);
            }
        }
    }

    public void AddItemToUI(Sprite itemSprite)
    {
        GameObject newIcon = Instantiate(itemIconPrefab, inventoryPanel);
        ItemIcon iconComponent = newIcon.GetComponent<ItemIcon>();
        iconComponent.SetIcon(itemSprite);
        itemIcons.Add(newIcon);
    }


}
