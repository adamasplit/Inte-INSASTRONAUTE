using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class EnemyPoolJsonExporter
{
    private const string SourceAssetPath = "Assets/Resources/STS/Enemies/EnemyPool.asset";
    private const string TargetJsonPath = "Assets/StreamingAssets/EnemyData/EnemyPool.json";

    [MenuItem("Tools/Export STS Enemy Pool to JSON")] 
    public static void ExportEnemyPoolToJson()
    {
        EnemyPool pool = AssetDatabase.LoadAssetAtPath<EnemyPool>(SourceAssetPath);
        if (pool == null)
        {
            Debug.LogError($"EnemyPool asset not found at '{SourceAssetPath}'.");
            return;
        }

        var wrapper = new EnemyPoolDTO();
        if (pool.enemies != null)
        {
            foreach (var entry in pool.enemies)
            {
                if (entry != null)
                {
                    wrapper.enemies.Add(entry.ToDTO());
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(TargetJsonPath));
        File.WriteAllText(TargetJsonPath, JsonConvert.SerializeObject(wrapper, Formatting.Indented));
        Debug.Log($"Exported {wrapper.enemies.Count} encounter entries to '{TargetJsonPath}'.");
    }

    [MenuItem("Tools/Import STS Enemy Pool from JSON")]
    public static void ImportEnemyPoolFromJson()
    {
        if (!File.Exists(TargetJsonPath))
        {
            Debug.LogError($"Enemy pool JSON not found at '{TargetJsonPath}'.");
            return;
        }

        string json = File.ReadAllText(TargetJsonPath);
        EnemyPoolDTO wrapper = JsonConvert.DeserializeObject<EnemyPoolDTO>(json);
        if (wrapper == null || wrapper.enemies == null)
        {
            Debug.LogError("Failed to deserialize enemy pool JSON.");
            return;
        }

        Dictionary<string, EnemyData> enemyLookup = BuildEnemyLookup();
        EnemyPool pool = AssetDatabase.LoadAssetAtPath<EnemyPool>(SourceAssetPath);
        bool isNewAsset = pool == null;
        if (isNewAsset)
        {
            pool = ScriptableObject.CreateInstance<EnemyPool>();
        }

        pool.enemies = new List<EncounterEntry>();
        foreach (EncounterEntryDTO dto in wrapper.enemies)
        {
            if (dto == null)
            {
                continue;
            }

            pool.enemies.Add(ResolveEncounterEntry(dto, enemyLookup));
        }

        if (isNewAsset)
        {
            AssetDatabase.CreateAsset(pool, SourceAssetPath);
        }

        EditorUtility.SetDirty(pool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Imported {pool.enemies.Count} encounter entries from '{TargetJsonPath}'.");
    }

    private static EncounterEntry ResolveEncounterEntry(EncounterEntryDTO dto, Dictionary<string, EnemyData> enemyLookup)
    {
        var entry = new EncounterEntry
        {
            displayName = dto.displayName,
            minFloor = dto.minFloor,
            maxFloor = dto.maxFloor,
            minAct = dto.minAct,
            maxAct = dto.maxAct,
            weight = dto.weight,
            elite = dto.elite,
            boss = dto.boss,
            enemies = new List<EnemyData>()
        };

        foreach (string enemyId in dto.enemyIds)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                continue;
            }

            if (enemyLookup.TryGetValue(enemyId, out EnemyData enemy))
            {
                entry.enemies.Add(enemy);
            }
            else
            {
                Debug.LogWarning($"Encounter '{dto.displayName}' references missing enemy '{enemyId}'.");
            }
        }

        return entry;
    }

    private static Dictionary<string, EnemyData> BuildEnemyLookup()
    {
        Dictionary<string, EnemyData> lookup = new(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:EnemyData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(enemy.id))
            {
                lookup[enemy.id] = enemy;
            }

            if (!string.IsNullOrWhiteSpace(enemy.enemyName))
            {
                lookup[enemy.enemyName] = enemy;
            }
        }

        return lookup;
    }
}