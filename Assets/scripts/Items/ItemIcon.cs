using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemIcon : MonoBehaviour
{
    private Image iconImage;

    void Awake()
    {
        iconImage = GetComponent<Image>();
    }

    public void SetIcon(Sprite newSprite)
    {
        if (iconImage != null && newSprite != null)
        {
            iconImage.sprite = newSprite;
        }
    }
}
