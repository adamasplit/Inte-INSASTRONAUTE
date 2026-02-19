using UnityEngine;
using System.Collections.Generic;
public class EnemySpawner : MonoBehaviour
{
    public Enemy enemyPrefab;
    public GridManager grid;
    private float nextSpawnTime = 0f;
    private float spawnInterval = 2f;
    public int spawnedEnemies = 1;
    public void Init()
    {
        nextSpawnTime = Time.time + spawnInterval;
        spawnedEnemies = 1;
    }
    void Update()
    {
        if (GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;
        spawnInterval = GameManager.Instance.spawnInterval;
        // Example spawning logic: spawn an enemy every 2 seconds in a random column
        if (Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;
            int enemyCount = Mathf.Min(Random.Range(1, spawnedEnemies + 1), 5); // Cap at 5 enemies per spawn
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy();
            }
        }
    }
        private void SpawnEnemy()
        {
            // Remove destroyed enemies from columns
            foreach (var col in grid.columns)
            {
                col.enemies.RemoveAll(e => e == null);
            }
            // Find available columns (no enemy at spawn position)
            List<int> availableColumns = new List<int>();
            for (int i = 0; i < grid.columns.Count; i++)
            {
                bool enemyPresent = false;
                foreach (var enemy in grid.columns[i].enemies)
                {
                    if (Vector3.Distance(enemy.transform.position, grid.columns[i].position) < 0.1f)
                    {
                        // Enemy already at spawn position
                        enemyPresent = true;
                    }
                }
                if (!enemyPresent)
                    availableColumns.Add(i);
                
            }

            if (availableColumns.Count == 0)
            {
                // No available columns, do not spawn
                return;
            }

            int column = availableColumns[Random.Range(0, availableColumns.Count)];
            Column selectedColumn = grid.columns[column];
            Enemy e = Instantiate(enemyPrefab, selectedColumn.enemyContainer);
            e.Initialize(100f, 0.5f+(GameManager.Instance.score*0.01f));
            selectedColumn.enemies.Add(e);
        }
}
