using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class EnemyDataExporter
{
    [MenuItem("Tools/Export STS Enemy Data")]
    public static void ExportEnemyData()
    {
        string folder = Path.Combine(StreamingAssetsLoader.GetStreamingAssetsRoot(), "EnemyData");
        Directory.CreateDirectory(folder);

        var guids = AssetDatabase.FindAssets("t:EnemyData");
        List<EnemyDataDTO> enemyDtos = new();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy == null)
            {
                continue;
            }

            EnemyDataDTO dto = enemy.ToDTO();
            string json = JsonConvert.SerializeObject(dto, Formatting.Indented);
            string outputPath = Path.Combine(folder, dto.id + ".json");

            File.WriteAllText(outputPath, json);
            enemyDtos.Add(dto);
        }

        List<string> enemyFiles = new();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy != null)
            {
                enemyFiles.Add((!string.IsNullOrEmpty(enemy.id) ? enemy.id : enemy.name) + ".json");
            }
        }

        enemyFiles.Sort(System.StringComparer.OrdinalIgnoreCase);

        string indexJson = JsonConvert.SerializeObject(new { files = enemyFiles }, Formatting.Indented);
        File.WriteAllText(Path.Combine(folder, "index.json"), indexJson);

        string enemiesJson = JsonConvert.SerializeObject(new EnemyDatabaseWrapper(enemyDtos), Formatting.Indented);
        File.WriteAllText(Path.Combine(folder, "enemies.json"), enemiesJson);

        AssetDatabase.Refresh();

        Debug.Log($"Exported {enemyDtos.Count} enemy data entries.");
    }

    [System.Serializable]
    private class EnemyDatabaseWrapper
    {
        public List<EnemyDataDTO> enemies;

        public EnemyDatabaseWrapper(List<EnemyDataDTO> enemies)
        {
            this.enemies = enemies;
        }
    }
}