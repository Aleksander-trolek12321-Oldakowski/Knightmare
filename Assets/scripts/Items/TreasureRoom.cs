using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureRoom : MonoBehaviour
{
    [SerializeField] private ItemSpawner[] itemSpawners;

    [SerializeField] private GameObject returnPortalPrefab;
    [SerializeField] private Vector3 portalSpawnPosition;


public void DestroyItem(GameObject collectedItem)
{
    foreach (var itemSpawner in itemSpawners)
    {
        if (itemSpawner != null && itemSpawner.gameObject != collectedItem)
        {
            var sp = itemSpawner.GetComponent<ScenePersistence>();
            if (sp != null)
                sp.RegisterRemoval();
            Destroy(itemSpawner.gameObject);
        }
    }
    GameData.Instance.SaveToDisk();
}
 

}
