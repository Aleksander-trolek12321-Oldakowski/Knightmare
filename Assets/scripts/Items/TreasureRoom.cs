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
        foreach (ItemSpawner itemSpawner in itemSpawners)
        {
            if (itemSpawner != null && itemSpawner.gameObject != collectedItem)
            {
                var spawnerPersist = itemSpawner.GetComponent<ScenePersistence>();
                if (spawnerPersist != null)
                    spawnerPersist.RegisterRemoval();

                Destroy(itemSpawner.gameObject);
            }
        }
    }
 

}
