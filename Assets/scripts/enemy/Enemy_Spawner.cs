using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace enemy
{
    public class Enemy_Spawner : MonoBehaviour
    {
        [SerializeField] private List<GameObject> enemyPrefabs;
        [SerializeField] private int[] enemyID;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private int minEnemies = 5;
        [SerializeField] private int maxEnemies = 15;
        [SerializeField] private float playerDetectionRadius = 10f;
        [SerializeField] private float minDistanceFromPlayer = 3f;
        [SerializeField] private float minDistanceBetweenEnemies = 2f;

        private Player player;
        private bool hasSpawned = false;

        private void Start()
        {
            player = FindFirstObjectByType<Player>();
        }

        private void Update()
        {
            if (hasSpawned || player == null)
                return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= playerDetectionRadius)
            {
                SpawnEnemies();
                hasSpawned = true;
            }
        }

        private void SpawnEnemies()
        {
            int numberOfEnemies = Random.Range(minEnemies, maxEnemies);

            for (int i = 0; i < numberOfEnemies; i++)
            {
                int id = Random.Range(0, enemyPrefabs.Count);
                Vector2 spawnPosition = FindValidSpawnPosition();

                if (spawnPosition != Vector2.zero)
                {
                    Instantiate(enemyPrefabs[id], spawnPosition, Quaternion.identity);
                    gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("Brak dostępnej pozycji do respienia przeciwnika.");
                }
            }
        }

        private Vector2 FindValidSpawnPosition()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 randomPosition = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

                if (Vector2.Distance(randomPosition, player.transform.position) < minDistanceFromPlayer)
                    continue;

                Collider2D[] colliders = Physics2D.OverlapCircleAll(randomPosition, minDistanceBetweenEnemies);
                bool isPositionValid = true;

                foreach (Collider2D collider in colliders)
                {
                    if (collider.GetComponent<enemy>() != null)
                    {
                        isPositionValid = false;
                        break;
                    }
                }

                if (isPositionValid)
                {
                    return randomPosition;
                }
            }

            return Vector2.zero;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
