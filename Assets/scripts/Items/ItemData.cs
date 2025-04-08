using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    public Sprite itemSprite;
    public float health;
    public float damage;
    public float speed;
    public float attackSpeed;
    public float range;
    public bool canPoison;
    public bool canFire;
    public bool canSlow;
    public int priceInShop;


}
