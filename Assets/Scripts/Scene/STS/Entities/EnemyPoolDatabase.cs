using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class EnemyPoolDatabase
{
    private static readonly string CombinedJsonPath = "EnemyPool/EnemyPool.json";

    static List<EncounterEntry> allEncounters;
    static List<float> actHpScaling = new();
    static bool isLoaded;
    static Task loadTask;

    public static int MaxAct { get; private set; } = -1;
    public static float BaseHpScaling { get; private set; } = 1f;
    public static IReadOnlyList<float> ActHpScaling => actHpScaling;

    public static bool IsLastAct(int act)
    {
        return MaxAct > 0 && act >= MaxAct - 1;
    }

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
        MaxAct = -1;
        BaseHpScaling = 1f;
        actHpScaling = new List<float>();

        Debug.Log("EnemyPoolDatabase loading remote enemy-pool catalog through React bridge first.");
        if (!await TryLoadFromRemoteApiAsync())
        {
            await LoadFromStreamingAssetsAsync();
        }

        isLoaded = allEncounters.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("EnemyPoolDatabase loaded zero encounter entries. Check the remote enemy-pool catalog and StreamingAssets/EnemyPool/EnemyPool.json.");
            allEncounters = null;
        }

        loadTask = null;
    }

    private static async Task<bool> TryLoadFromRemoteApiAsync()
    {
        Debug.Log("EnemyPoolDatabase requesting enemy-pool catalog (sts.catalog.enemy-pool) through React bridge.");
        string json = await ReactApiBridge.RequestStsCatalogEnemyPoolAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("EnemyPoolDatabase did not receive an enemy-pool payload from the React bridge.");
            return false;
        }

        Debug.Log($"EnemyPoolDatabase received enemy-pool payload from React bridge ({json.Length} chars).");

        try
        {
            EnemyPoolDTO remotePool = ParseRemoteEnemyPool(json);
            if (remotePool == null)
            {
                Debug.LogWarning("EnemyPoolDatabase could not parse an enemy-pool object in the React bridge payload.");
                return false;
            }

            ApplyPool(remotePool, "remote API");
            return allEncounters.Count > 0;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load enemy pool through the React bridge: {ex}");
            return false;
        }
    }

    private static async Task LoadFromStreamingAssetsAsync()
    {
        Debug.Log("EnemyPoolDatabase falling back to StreamingAssets/EnemyPool/EnemyPool.json.");

        string combinedJson = await StreamingAssetsLoader.ReadAllTextAsync(CombinedJsonPath);
        if (!string.IsNullOrEmpty(combinedJson))
        {
            try
            {
                EnemyPoolDTO wrapper = JsonConvert.DeserializeObject<EnemyPoolDTO>(combinedJson);
                if (wrapper != null)
                {
                    ApplyPool(wrapper, "StreamingAssets/EnemyPool/EnemyPool.json");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load combined EnemyPool.json: {ex}");
            }
        }

        Debug.LogWarning("EnemyPoolDatabase could not read a valid combined EnemyPool.json payload.");
    }

    private static EnemyPoolDTO ParseRemoteEnemyPool(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        JToken root = JToken.Parse(json);
        JToken poolToken = FindEnemyPoolToken(root);
        if (poolToken == null || poolToken.Type != JTokenType.Object)
            return null;

        return poolToken.ToObject<EnemyPoolDTO>();
    }

    private static JToken FindEnemyPoolToken(JToken token)
    {
        if (token == null)
            return null;

        if (token.Type == JTokenType.Object)
        {
            JObject obj = (JObject)token;
            if (obj.TryGetValue("enemies", StringComparison.OrdinalIgnoreCase, out JToken enemiesToken) &&
                enemiesToken != null && enemiesToken.Type == JTokenType.Array)
            {
                return obj;
            }

            string[] candidateKeys = new[] { "enemyPool", "pool", "data", "result", "payload" };
            foreach (string key in candidateKeys)
            {
                if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nestedToken))
                {
                    JToken found = FindEnemyPoolToken(nestedToken);
                    if (found != null)
                        return found;
                }
            }

            foreach (JProperty property in obj.Properties())
            {
                JToken found = FindEnemyPoolToken(property.Value);
                if (found != null)
                    return found;
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (JToken item in token)
            {
                JToken found = FindEnemyPoolToken(item);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private static void ApplyPool(EnemyPoolDTO poolDto, string sourceName)
    {
        if (poolDto == null)
            return;

        MaxAct = poolDto.maxAct;
        BaseHpScaling = poolDto.baseHpScaling;
        actHpScaling = poolDto.actHpScaling != null ? new List<float>(poolDto.actHpScaling) : new List<float>();

        int encounterCount = poolDto.enemies != null ? poolDto.enemies.Count : 0;
        Debug.Log($"EnemyPoolDatabase loaded {encounterCount} encounter entries from {sourceName}.");

        if (poolDto.enemies == null)
            return;

        foreach (EncounterEntryDTO dto in poolDto.enemies)
        {
            if (dto == null)
                continue;

            EncounterEntry entry = EncounterEntry.FromDTO(dto);
            allEncounters.Add(entry);
        }
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
            {
                Debug.Log($"Selected encounter: {entry.displayName} (Floor {floor}, Elite: {elite}, Boss: {boss})");
                return entry.enemies;
            }
        }

        return candidates[0].enemies;
    }
}