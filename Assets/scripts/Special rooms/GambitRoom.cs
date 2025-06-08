using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GambitRoom : MonoBehaviour
{
    public ItemData[] possibleItems;
    public float itemChangeInterval = 10f;

    private ItemData currentItem;
    private SpriteRenderer spriteRenderer;
    private Coroutine changeCoroutine;

    [SerializeField] private GameObject returnPortalPrefab;
    [SerializeField] private Vector3 portalSpawnPosition;
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        changeCoroutine = StartCoroutine(ChangeItemPeriodically());
    }

    private IEnumerator ChangeItemPeriodically()
    {
        while (true)
        {
            GenerateRandomItem();
            yield return new WaitForSeconds(itemChangeInterval);
        }
    }

    private void GenerateRandomItem()
    {
        if (possibleItems.Length == 0) return;

        int randomIndex = Random.Range(0, possibleItems.Length);
        currentItem = possibleItems[randomIndex];

        ApplyItemData();
    }

    private void ApplyItemData()
    {
        if (spriteRenderer != null && currentItem != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = currentItem.itemSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            player.ReplaceItem(currentItem);

            player.ApplyItemStats(currentItem);

            if (currentItem.itemSprite != null)
            {
                InventoryUI.Instance.RemoveLastItemIcon();

                InventoryUI.Instance.AddItemToUI(currentItem.itemSprite, currentItem.name);
                GameData.Instance.collectedItemIcons.Add(currentItem.itemSprite);
            }

            if (changeCoroutine != null)
                StopCoroutine(changeCoroutine);
            CreateReturnPortal();
            Destroy(gameObject);
        }
    }
    private void CreateReturnPortal()
    {
        Instantiate(returnPortalPrefab, portalSpawnPosition, Quaternion.identity);
    }
}
