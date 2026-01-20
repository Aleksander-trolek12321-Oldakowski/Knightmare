#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

public static class PopulateItemDatabase
{
    [MenuItem("Tools/Populate ItemDatabase (scan project)")]
    public static void Populate()
    {
        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string dbPath = "Assets/Resources/ItemDatabase.asset";
        ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Utworzono ItemDatabase w Resources.");
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        var items = guids.Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
                         .Where(x => x != null).ToList();

        db.allItems = items;
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"ItemDatabase zaktualizowany. Znaleziono {items.Count} ItemData.");
    }
}
#endif
