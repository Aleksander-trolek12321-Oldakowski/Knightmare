using System;
using UnityEngine;

public class ScenePersistence : MonoBehaviour
{
    [Tooltip("Unique ID for this object or spawner")]
    public string uniqueID;

    [Tooltip("If true, ID is saved/loaded in destroyedSpawnerIDs; if false, in destroyedPortals.")]
    public bool isSpawner = false;

    private void Awake()
    {
        if (GameData.Instance == null)
            return;

        var list = isSpawner
                 ? GameData.Instance.destroyedSpawnerIDs
                 : GameData.Instance.destroyedPortals;

        if (list.Contains(uniqueID))
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[ScenePersistence] Awake on '{uniqueID}'. Should remove? {list.Contains(uniqueID)}");
    }


    public void RegisterRemoval()
    {
        if (GameData.Instance == null)
            return;

        var list = isSpawner
                 ? GameData.Instance.destroyedSpawnerIDs
                 : GameData.Instance.destroyedPortals;

        if (!list.Contains(uniqueID))
        {
            list.Add(uniqueID);
            GameData.Instance.SaveToDisk();
        }

        Destroy(gameObject);
    }
}