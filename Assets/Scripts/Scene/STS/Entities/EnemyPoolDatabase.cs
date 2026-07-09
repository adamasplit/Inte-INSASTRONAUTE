using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public static class EnemyPoolDatabase
{
    private static readonly string CombinedJsonPath = "EnemyData/EnemyPool.json";

    static List<EncounterEntry> allEncounters;
    static bool isLoaded;
    static Task loadTask;

    public static async Task LoadAsync()
    {
        if (loadTask != null)
        {
            await loadTask;
            return;
        }

        loadTask = LoadInternalAsync();
        await loadTask;
    }

    private static async Task LoadInternalAsync()
    {
        if (isLoaded)
            return;

        await EnemyDataDatabase.LoadAsync();

        allEncounters = new List<EncounterEntry>();

        string combinedJson = await StreamingAssetsLoader.ReadAllTextAsync(CombinedJsonPath);
        if (!string.IsNullOrEmpty(combinedJson))
        {
            try
            {
                EnemyPoolDTO wrapper = JsonConvert.DeserializeObject<EnemyPoolDTO>(combinedJson);
                if (wrapper != null && wrapper.enemies != null)
                {
                    Debug.Log($"EnemyPoolDatabase loaded {wrapper.enemies.Count} encounter entries from EnemyPool.json.");
                    foreach (EncounterEntryDTO dto in wrapper.enemies)
                    {
                        if (dto == null)
                        {
                            continue;
                        }

                        EncounterEntry entry = EncounterEntry.FromDTO(dto);
                        allEncounters.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load combined EnemyPool.json: {ex}");
            }
        }

        isLoaded = allEncounters.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("EnemyPoolDatabase loaded zero encounter entries. Check StreamingAssets/EnemyData/EnemyPool.json.");
            allEncounters = null;
        }

        loadTask = null;
    }

    public static void Load()
    {
#if UNITY_ANDROID || UNITY_WEBGL
        Debug.LogError("EnemyPoolDatabase.Load() is not supported on Android/WebGL. Use LoadAsync() and await it.");
#else
        LoadAsync().GetAwaiter().GetResult();
#endif
    }

    public static async Task EnsureLoadedAsync()
    {
        if (allEncounters != null && allEncounters.Count > 0)
            return;

        isLoaded = false;
        await LoadAsync();
    }

    public static List<EnemyData> GetRandomEncounter(int floor, bool elite = false, bool boss = false)
    {
        if (allEncounters == null || allEncounters.Count == 0)
        {
            Load();
        }

        var candidates = allEncounters
            .Where(e =>
                e.minFloor <= floor &&
                (e.minAct == -1 || e.minAct <= RunManager.Instance.act) &&
                (e.maxAct == -1 || e.maxAct >= RunManager.Instance.act) &&
                (e.maxFloor >= floor || e.maxFloor == -1) &&
                e.elite == elite &&
                e.boss == boss)
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogError("No enemy found for this config");
            return null;
        }

        float totalWeight = candidates.Sum(c => c.weight);
        float roll = UnityEngine.Random.value * totalWeight;
        float current = 0;

        foreach (var entry in candidates)
        {
            current += entry.weight;
            if (roll <= current)
                return entry.enemies;
        }

        return candidates[0].enemies;
    }
}