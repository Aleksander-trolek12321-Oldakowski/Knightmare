using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemySpace
{
    [RequireComponent(typeof(ScenePersistence))]
    public class Enemy_Spawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public string uniqueSpawnerID;
        public List<GameObject> enemyPrefabs;
        public int minEnemies = 5;
        public int maxEnemies = 15;
        public float spawnRadius = 10f;

        [Header("Spawn Conditions")]
        public float playerDetectionRadius = 10f;
        public float minDistanceFromPlayer = 3f;
        public float minDistanceBetweenEnemies = 2f;

        private Player player;
        private bool hasSpawned = false;
        private ScenePersistence persistence;

        void Awake()
        {
            persistence = GetComponent<ScenePersistence>();
            persistence.uniqueID = uniqueSpawnerID;
            persistence.isSpawner = true;
        }

        void Start()
        {
            // Restore previously spawned enemies if any
            if (GameData.Instance != null && GameData.Instance.enemyStates.Exists(s => s.uniqueID.StartsWith(uniqueSpawnerID + "_")))
            {
                foreach (var es in GameData.Instance.enemyStates)
                {
                    if (!es.uniqueID.StartsWith(uniqueSpawnerID + "_"))
                        continue;

                    // Use saved prefabIndex
                    int idx = es.prefabIndex;
                    if (idx < 0 || idx >= enemyPrefabs.Count)
                        continue;

                    // Instantiate at saved position
                    GameObject go = Instantiate(enemyPrefabs[idx], es.position.ToVector3(), Quaternion.identity);
                    var en = go.GetComponent<enemy>();
                    if (en != null)
                        en.uniqueID = es.uniqueID;
                }
                hasSpawned = true;
                persistence.RegisterRemoval();
                return;
            }

            // Normal flow
            player = FindObjectOfType<Player>();
            if (player == null)
                Debug.LogError("Player not found in scene.");
        }

        void Update()
        {
            if (hasSpawned || player == null)
                return;

            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= playerDetectionRadius)
            {
                SpawnEnemies();
                hasSpawned = true;
                persistence.RegisterRemoval();
            }
        }

        private void SpawnEnemies()
        {
            // Clear any previous states for this spawner to avoid duplicates
            if (GameData.Instance != null)
            {
                //GameData.Instance.enemyStates.RemoveAll(es => es.uniqueID.StartsWith(uniqueSpawnerID + "_"));
            }

            if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            {
                Debug.LogError("Enemy prefabs list is empty!");
                return;
            }

            int count = Random.Range(minEnemies, maxEnemies + 1);
            Debug.Log($"{uniqueSpawnerID} spawning {count} enemies.");
            {
                if (enemyPrefabs == null || enemyPrefabs.Count == 0)
                {
                    Debug.LogError("Enemy prefabs list is empty!");
                    return;
                }

                Debug.Log($"{uniqueSpawnerID} spawning {count} enemies.");

                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = GetValidSpawnPosition();
                    if (pos == Vector2.zero)
                    {
                        Debug.LogWarning("No valid spawn position found.");
                        continue;
                    }

                    int idx = Random.Range(0, enemyPrefabs.Count);
                    GameObject go = Instantiate(enemyPrefabs[idx], pos, Quaternion.identity);
                    var en = go.GetComponent<enemy>();
                    if (en == null)
                    {
                        Debug.LogError("Spawned prefab missing 'enemy' component.");
                        Destroy(go);
                        continue;
                    }

                    // Assign uniqueID combining spawner and index
                    en.uniqueID = $"{uniqueSpawnerID}_{i}";
                    Debug.Log($"Registered {en.uniqueID} with prefab index {idx}");

                    // Record state
                    /*if (GameData.Instance != null)
                    {
                        GameData.Instance.enemyStates.Add(new EnemyState {
                            uniqueID = en.uniqueID,
                            position = new SerializableVector3(en.transform.position),
                            prefabIndex = idx
                        });
                    }
                }

                // Save all recorded enemy states
                if (GameData.Instance != null)
                {
                    Debug.Log($"Saving {GameData.Instance.enemyStates.Count} enemyStates");
                    GameData.Instance.SaveToDisk();
                }*/
            }
        }

        private Vector2 GetValidSpawnPosition()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 candidate = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
                if (Vector2.Distance(candidate, player.transform.position) < minDistanceFromPlayer)
                    continue;
                var hit = Physics2D.OverlapCircle(candidate, 0.5f);
                if (hit != null && hit.GetComponent<TilemapCollider2D>() != null)
                    continue;
                var others = Physics2D.OverlapCircleAll(candidate, minDistanceBetweenEnemies);
                bool ok = true;
                foreach (var c in others)
                {
                    if (c.GetComponent<enemy>() != null)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok) return candidate;
            }
            return Vector2.zero;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
        }
    }
}


