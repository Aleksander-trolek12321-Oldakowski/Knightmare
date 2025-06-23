using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TutorialPopup : MonoBehaviour 
{
    [Tooltip("Unique ID for the targetObject persistence")]
    public string targetID;

    private ScenePersistence targetPersistence;
    void Awake()
    {
        targetPersistence = GetComponent<ScenePersistence>();
        targetPersistence.uniqueID = targetID;
        targetPersistence.isSpawner = false;
    }
    void OnDestroy()
    {
        targetPersistence.RegisterRemoval();
    }
}

