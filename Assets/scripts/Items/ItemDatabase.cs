using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems = new List<ItemData>();

    // helper lookup by name
    public ItemData GetByName(string name)
    {
        return allItems.Find(i => i != null && i.name == name);
    }

    public string[] GetAllNames()
    {
        var list = new List<string>();
        foreach (var i in allItems) if (i != null) list.Add(i.name);
        return list.ToArray();
    }
}
