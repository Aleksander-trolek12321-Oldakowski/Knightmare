using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapRoom : MonoBehaviour
{
    [System.Serializable]
    public class EnemyWave
    {
        public GameObject[] enemies;
    }

    [Header("Fale przeciwników")]
    [SerializeField] private EnemyWave[] waves;
    private int currentWaveIndex = 0;

    [Header("Ustawienia portalu")]
    [SerializeField] private GameObject returnPortalPrefab;
    [SerializeField] private Vector3 portalSpawnPosition;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Start()
    {
        SpawnWave(currentWaveIndex);
    }

    private void Update()
    {
        if (spawnedEnemies.Count > 0 && AllEnemiesDefeated())
        {
            currentWaveIndex++;

            if (currentWaveIndex < waves.Length)
            {
                SpawnWave(currentWaveIndex);
            }
            else
            {
                CreateReturnPortal();
            }
        }
    }

    private void SpawnWave(int waveIndex)
    {
        spawnedEnemies.Clear();

        foreach (GameObject enemyPrefab in waves[waveIndex].enemies)
        {
            GameObject enemy = Instantiate(enemyPrefab, GetRandomPosition(), Quaternion.identity);
            spawnedEnemies.Add(enemy);
        }
    }

    private bool AllEnemiesDefeated()
    {
        spawnedEnemies.RemoveAll(e => e == null);
        return spawnedEnemies.Count == 0;
    }

    private Vector3 GetRandomPosition()
    {
        return transform.position + new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), 0);
    }

    private void CreateReturnPortal()
    {
        Instantiate(returnPortalPrefab, portalSpawnPosition, Quaternion.identity);
    }
}
