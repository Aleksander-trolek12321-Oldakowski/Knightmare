using System.Collections.Generic;
using UnityEngine;
using enemySpace;

public class SceneEnemyLoader : MonoBehaviour
{
    [Tooltip("Lista prefabów wrogów w tej samej kolejności, co podczas zapisu.")]
    public List<GameObject> enemyPrefabs;

    void Start()
    {
        if (GameData.Instance == null)
            return;

        var states = GameData.Instance.enemyStates;
        Debug.Log($"[SceneEnemyLoader] Restoring {states.Count} enemies.");

        foreach (var es in states)
        {
            // Prefab index z zapisu
            int idx = es.prefabIndex;
            Debug.Log($"[Loader] Przywracam {es.uniqueID} z prefabIndex = {es.prefabIndex}");
            if (idx < 0 || idx >= enemyPrefabs.Count)
            {
                Debug.LogWarning($"Invalid prefabIndex {idx} for {es.uniqueID}");
                continue;
            }

            // Tworzymy wroga na zapositionowanych współrzędnych
            var go = Instantiate(enemyPrefabs[idx], es.position.ToVector3(), Quaternion.identity);
            var en = go.GetComponent<enemy>();
            if (en != null)
                en.uniqueID = es.uniqueID;
        }

        // Clear so we don't double-restore
        GameData.Instance.enemyStates.Clear();
    }
}
