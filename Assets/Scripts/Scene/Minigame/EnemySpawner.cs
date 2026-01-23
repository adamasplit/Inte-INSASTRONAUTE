using UnityEngine;
using System.Collections.Generic;
public class EnemySpawner : MonoBehaviour
{
    public Enemy enemyPrefab;
    public GridManager grid;

    void Update()
    {
        if (GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        // Example spawning logic: spawn an enemy every 2 seconds in a random column
        if (Time.frameCount % GameManager.Instance.spawnInterval == 0)
        {
            int columnIndex = Random.Range(0, GameManager.Instance.columnCount);
            SpawnEnemy();
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
            e.Initialize(100f, 50f);
            selectedColumn.enemies.Add(e);
        }

    void SpawnEnemy(int columnIndex)
    {
        Column col = grid.columns[columnIndex];
        Enemy e = Instantiate(enemyPrefab, col.enemyContainer);
        e.Initialize(100f, 50f);
        col.enemies.Add(e);
    }
}
