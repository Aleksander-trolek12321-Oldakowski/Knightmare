using UnityEngine;

/// <summary>
/// Controls a one-time tutorial event by disabling a target object permanently via ScenePersistence.
/// </summary>
public class Tutorial : MonoBehaviour
{
    [Tooltip("Target object to disable permanently after trigger")]
    public GameObject targetObject;

    [Tooltip("Unique ID for this tutorial instance (optional)")]
    public string tutorialID;

    [Tooltip("Unique ID for the targetObject persistence")]
    public string targetID;

    private ScenePersistence targetPersistence;

    private void Awake()
    {
        var selfPersist = GetComponent<ScenePersistence>();
        selfPersist.uniqueID = tutorialID;
        selfPersist.isSpawner = false;

        if (targetObject != null)
        {
            targetPersistence = targetObject.GetComponent<ScenePersistence>();
            if (targetPersistence == null)
                targetPersistence = targetObject.AddComponent<ScenePersistence>();

            targetPersistence.uniqueID = targetID;
            targetPersistence.isSpawner = false;
        }
    }

    private void Start()
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == 3 && targetObject != null)
        {
            targetObject.SetActive(false);
            targetPersistence.RegisterRemoval();

            Destroy(targetObject);
            Destroy(this.gameObject);
        }
    }
}