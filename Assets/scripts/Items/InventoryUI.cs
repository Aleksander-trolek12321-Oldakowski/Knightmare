using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("UI Prefabs & Panels")]
    [SerializeField] private GameObject itemIconPrefab;
    [SerializeField] private Transform inventoryPanel;

    [Header("Item Name Popup")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private CanvasGroup itemNameCanvasGroup;
    [SerializeField] private float nameDisplayDuration = 1f;

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

        if (itemNameCanvasGroup != null)
        {
            itemNameCanvasGroup.alpha = 0f;
            itemNameCanvasGroup.interactable = false;
            itemNameCanvasGroup.blocksRaycasts = false;
        }
    }

    public void AddItemToUI(Sprite itemSprite, string itemName)
    {
        GameObject newIcon = Instantiate(itemIconPrefab, inventoryPanel);
        ItemIcon iconComponent = newIcon.GetComponent<ItemIcon>();
        iconComponent.SetIcon(itemSprite);
        itemIcons.Add(newIcon);

        if (itemNameText != null && itemNameCanvasGroup != null)
        {
            StopAllCoroutines(); // optional: cancel previous popup
            StartCoroutine(ShowItemNameCoroutine(itemName));
        }
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

    private IEnumerator ShowItemNameCoroutine(string itemName)
    {
        itemNameText.text = itemName;
        yield return StartCoroutine(FadeCanvasGroup(itemNameCanvasGroup, 0f, 1f, 0.2f));

        yield return new WaitForSeconds(nameDisplayDuration);

        yield return StartCoroutine(FadeCanvasGroup(itemNameCanvasGroup, 1f, 0f, 0.2f));
    }
    
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cg.alpha = end;
        yield break;
    }
}