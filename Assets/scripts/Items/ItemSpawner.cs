using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ItemSpawner : MonoBehaviour
{
    public ItemData[] possibleItems;
    public GameObject itemPrefab;

    void Start()
    {
        GenerateRandomItem();
    }

    private void GenerateRandomItem()
    {
        int randomIndex = Random.Range(0, possibleItems.Length);
        ItemData selectedItem = possibleItems[randomIndex];

        GameObject newItem = Instantiate(itemPrefab, transform.position, Quaternion.identity);
        Item itemComponent = newItem.GetComponent<Item>();

        itemComponent.itemData = selectedItem;
        itemComponent.ApplyItemData();

        Destroy(gameObject);
    }
}
