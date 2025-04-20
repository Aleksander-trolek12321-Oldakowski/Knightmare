using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPortal : MonoBehaviour
{  
    

    [SerializeField] private GameObject returnPortalPrefab;

    [SerializeField] private Vector3 portalSpawnPosition;

    [SerializeField] private bool isPortalCreated = false;

    public void CreateReturnPortal()
    {
        if (!isPortalCreated)
        {
            Instantiate(returnPortalPrefab, portalSpawnPosition, Quaternion.identity);

        }
    }
}
