using UnityEngine;

public class ScenePersistence : MonoBehaviour
{
    [Tooltip("Unikalne ID tego obiektu lub spawnera")]
    public string uniqueID;

    [Tooltip("Jeśli true, ID trafi do destroyedPortals; jeśli false — to spawner/item z destroyedSpawnerIDs")]
    public bool isSpawner = false;

    void Awake()
    {
        var list = isSpawner
                 ? GameData.Instance.destroyedSpawnerIDs
                 : GameData.Instance.destroyedPortals;

        if (list.Contains(uniqueID))
        {
            if (isSpawner)
                gameObject.SetActive(false);
            else
                Destroy(gameObject);
        }
    }

    public void RegisterRemoval()
    {
        var list = isSpawner
                 ? GameData.Instance.destroyedSpawnerIDs
                 : GameData.Instance.destroyedPortals;

        if (!list.Contains(uniqueID))
        {
            list.Add(uniqueID);
            GameData.Instance.SaveToDisk();
        }

        // Natychmiast ukryj/deaktywuj:
        gameObject.SetActive(false);
    }
}
