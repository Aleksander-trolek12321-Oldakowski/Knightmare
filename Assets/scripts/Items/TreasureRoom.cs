using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureRoom : MonoBehaviour
{
    [SerializeField] private ItemSpawner[] itemSpawners;




    public void DestroyItem(GameObject collectedItem)
    {


        foreach (ItemSpawner itemSpawner in itemSpawners)
        {
            if (itemSpawner != null && itemSpawner.gameObject != collectedItem)
            {
                Destroy(itemSpawner.gameObject);
            }
        }
    }

 
}
