using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System;

public static class EnemyDataDatabase
{
    [System.Serializable]
    private class EnemyDataWrapper
    {
        public List<EnemyDataDTO> enemies;
    }

    static Dictionary<string, EnemyData> enemyDict;
    static bool isLoaded;
    static Task loadTask;

    public static List<EnemyData> allEnemies;

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

    static async Task LoadInternalAsync()
    {
        if (isLoaded)
            return;

        await STSCardDatabase.EnsureLoadedAsync();

        enemyDict = new();
        allEnemies = new();

        Debug.Log("EnemyDataDatabase loading enemy catalog through React bridge first.");

        string json = await ReactApiBridge.RequestStsCatalogEnemiesAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("EnemyDataDatabase did not receive an enemy catalog payload from the React bridge; falling back to StreamingAssets.");
            await LoadFromStreamingAssetsAsync();
            isLoaded = allEnemies.Count > 0;
            if (!isLoaded)
            {
                Debug.LogError("EnemyDataDatabase loaded zero enemies from both the React bridge and StreamingAssets.");
                enemyDict = null;
                allEnemies = null;
            }

            loadTask = null;
            return;
        }

        Debug.Log($"EnemyDataDatabase received enemy catalog payload from React bridge ({json.Length} chars).");

        try
        {
            List<EnemyDataDTO> remoteEnemies = ParseRemoteEnemies(json);
            if (remoteEnemies == null || remoteEnemies.Count == 0)
            {
                Debug.LogWarning("EnemyDataDatabase could not parse a usable enemy array from the React bridge payload; falling back to StreamingAssets.");
                await LoadFromStreamingAssetsAsync();
            }
            else
            {
                Debug.Log($"EnemyDataDatabase parsed {remoteEnemies.Count} enemy entries from the remote payload.");

                foreach (EnemyDataDTO dto in remoteEnemies)
                {
                    if (dto == null)
                    {
                        Debug.LogWarning("EnemyDataDatabase encountered a null enemy DTO in the remote payload.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(dto.id) && string.IsNullOrWhiteSpace(dto.enemyName))
                    {
                        Debug.LogWarning("EnemyDataDatabase encountered an enemy DTO without id or enemyName in the remote payload.");
                    }

                    try
                    {
                        EnemyData enemy = EnemyData.FromDTO(dto);
                        RegisterEnemy(enemy);
                        allEnemies.Add(enemy);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"EnemyDataDatabase skipped remote enemy '{dto.id ?? dto.enemyName ?? "<unknown>"}' because it could not be converted: {ex.Message}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to load enemies through the React bridge: {ex}; falling back to StreamingAssets.");
            await LoadFromStreamingAssetsAsync();
        }

        isLoaded = allEnemies.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("EnemyDataDatabase loaded zero enemies from both the React bridge and StreamingAssets.");
            enemyDict = null;
            allEnemies = null;
        }

        loadTask = null;
    }

    static List<EnemyDataDTO> ParseRemoteEnemies(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        JToken root = JToken.Parse(json);
        return ParseRemoteEnemies(root);
    }

    static List<EnemyDataDTO> ParseRemoteEnemies(JToken token)
    {
        if (token == null)
            return null;

        if (token.Type == JTokenType.Array)
        {
            JArray array = (JArray)token;
            List<EnemyDataDTO> enemies = new List<EnemyDataDTO>(array.Count);

            foreach (JToken item in array)
            {
                EnemyDataDTO dto = ParseEnemyDto(item);
                if (dto != null)
                {
                    enemies.Add(dto);
                }
            }

            return enemies;
        }

        if (token.Type != JTokenType.Object)
            return null;

        JObject rootObject = (JObject)token;
        string[] candidateKeys = new[] { "enemies", "data", "items", "result", "payload" };

        foreach (string key in candidateKeys)
        {
            if (rootObject.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nestedToken))
            {
                List<EnemyDataDTO> nestedEnemies = ParseRemoteEnemies(nestedToken);
                if (nestedEnemies != null && nestedEnemies.Count > 0)
                {
                    return nestedEnemies;
                }
            }
        }

        foreach (JProperty property in rootObject.Properties())
        {
            if (property.Value.Type != JTokenType.Object && property.Value.Type != JTokenType.Array)
                continue;

            List<EnemyDataDTO> nestedEnemies = ParseRemoteEnemies(property.Value);
            if (nestedEnemies != null && nestedEnemies.Count > 0)
            {
                return nestedEnemies;
            }
        }

        return null;
    }

    static async Task LoadFromStreamingAssetsAsync()
    {
        Debug.Log("EnemyDataDatabase falling back to StreamingAssets/EnemyData.");

        string combinedJson = await StreamingAssetsLoader.ReadAllTextAsync("EnemyData/enemies.json");
        if (!string.IsNullOrEmpty(combinedJson))
        {
            try
            {
                EnemyDataWrapper wrapper = JsonConvert.DeserializeObject<EnemyDataWrapper>(combinedJson);
                if (wrapper != null && wrapper.enemies != null)
                {
                    Debug.Log($"EnemyDataDatabase loaded {wrapper.enemies.Count} enemies from enemies.json.");
                    foreach (EnemyDataDTO dto in wrapper.enemies)
                    {
                        if (dto == null)
                        {
                            Debug.LogWarning("EnemyDataDatabase encountered a null enemy DTO in enemies.json.");
                            continue;
                        }

                        EnemyData enemy = EnemyData.FromDTO(dto);
                        RegisterEnemy(enemy);
                        allEnemies.Add(enemy);
                    }

                    Debug.Log($"EnemyDataDatabase streaming-assets combined file load completed with {allEnemies.Count} enemies.");
                    return;
                }

                Debug.LogWarning("EnemyDataDatabase combined enemies.json did not contain an enemies array.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load combined enemies.json: {ex}");
            }
        }
        else
        {
            Debug.LogWarning("EnemyDataDatabase could not read StreamingAssets/EnemyData/enemies.json; falling back to per-file loading.");
        }

        List<string> files = await StreamingAssetsLoader.ListJsonFilesAsync("EnemyData");
        Debug.Log($"EnemyDataDatabase found {files.Count} enemy JSON files.");

        foreach (string file in files)
        {
            try
            {
                string fileJson = await StreamingAssetsLoader.ReadAllTextAsync(file);
                if (string.IsNullOrEmpty(fileJson))
                    continue;

                EnemyDataDTO dto = JsonConvert.DeserializeObject<EnemyDataDTO>(fileJson);

                if (dto == null)
                {
                    Debug.LogWarning($"Invalid enemy JSON in '{file}'.");
                    continue;
                }

                EnemyData enemy = EnemyData.FromDTO(dto);
                RegisterEnemy(enemy);
                allEnemies.Add(enemy);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load enemy '{file}': {ex}");
            }
        }

        Debug.Log($"EnemyDataDatabase per-file StreamingAssets load completed with {allEnemies.Count} enemies.");
    }

    static EnemyDataDTO ParseEnemyDto(JToken token)
    {
        if (token == null || token.Type != JTokenType.Object)
            return null;

        JObject obj = (JObject)token;
        EnemyDataDTO dto = new EnemyDataDTO
        {
            id = ReadString(obj, "id", "name"),
            enemyName = ReadString(obj, "enemyName", "name", "displayName"),
            displayName = ReadString(obj, "displayName", "name", "enemyName"),
            maxHP = ReadInt(obj, 0, "maxHP", "hp", "health"),
            randomStart = ReadBool(obj, false, "randomStart"),
            startingStatus = ReadString(obj, "startingStatus"),
            startingStatusInfo = ReadString(obj, "startingStatusInfo", "startingStatusDescription", "statusInfo"),
            startingStatusDuration = ReadInt(obj, 0, "startingStatusDuration", "statusDuration"),
            startingStatusValue = ReadInt(obj, 0, "startingStatusValue", "statusValue")
        };

        dto.patternCardIds = ReadStringList(obj, "patternCardIds", "patternCards", "pattern");
        dto.rewardCardIds = ReadStringList(obj, "rewardCardIds", "rewardCards", "rewards");
        dto.movePattern = ReadMovePatternList(obj, "movePattern", "moves", "patternMoves");

        if (string.IsNullOrWhiteSpace(dto.id) && string.IsNullOrWhiteSpace(dto.enemyName) && string.IsNullOrWhiteSpace(dto.displayName))
            return null;

        return dto;
    }

    static string ReadString(JObject obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken value) && value != null && value.Type != JTokenType.Null)
            {
                string result = value.ToString();
                if (!string.IsNullOrWhiteSpace(result))
                    return result;
            }
        }

        return null;
    }

    static int ReadInt(JObject obj, int defaultValue, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken value) || value == null || value.Type == JTokenType.Null)
                continue;

            if (value.Type == JTokenType.Integer || value.Type == JTokenType.Float)
                return value.Value<int>();

            if (int.TryParse(value.ToString(), out int parsed))
                return parsed;
        }

        return defaultValue;
    }

    static bool ReadBool(JObject obj, bool defaultValue, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken value) || value == null || value.Type == JTokenType.Null)
                continue;

            if (value.Type == JTokenType.Boolean)
                return value.Value<bool>();

            if (bool.TryParse(value.ToString(), out bool parsed))
                return parsed;
        }

        return defaultValue;
    }

    static List<string> ReadStringList(JObject obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken value) || value == null)
                continue;

            if (value.Type == JTokenType.Array)
            {
                List<string> items = new List<string>();
                foreach (JToken entry in value)
                {
                    string text = entry?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        items.Add(text);
                    }
                }

                return items;
            }
        }

        return new List<string>();
    }

    static List<EnemyMoveEntryDTO> ReadMovePatternList(JObject obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken value) || value == null)
                continue;

            if (value.Type != JTokenType.Array)
                continue;

            JArray array = (JArray)value;
            List<EnemyMoveEntryDTO> moves = new List<EnemyMoveEntryDTO>(array.Count);

            foreach (JToken entry in array)
            {
                if (entry == null || entry.Type != JTokenType.Object)
                    continue;

                JObject moveObj = (JObject)entry;
                EnemyMoveEntryDTO move = new EnemyMoveEntryDTO
                {
                    cardId = ReadString(moveObj, "cardId", "cardID", "name"),
                    moveName = ReadString(moveObj, "moveName", "name"),
                    effects = moveObj["effects"] != null ? moveObj["effects"].ToObject<List<EffectEntryDTO>>() : new List<EffectEntryDTO>(),
                    nextMoveIndices = moveObj["nextMoveIndices"] != null ? moveObj["nextMoveIndices"].ToObject<List<int>>() : new List<int>(),
                    weight = ReadInt(moveObj, 1, "weight")
                };

                moves.Add(move);
            }

            return moves;
        }

        return new List<EnemyMoveEntryDTO>();
    }

    static void RegisterEnemy(EnemyData enemy)
    {
        if (enemy == null)
            return;

        if (!string.IsNullOrEmpty(enemy.id))
        {
            enemyDict[enemy.id] = enemy;
        }

        if (!string.IsNullOrEmpty(enemy.enemyName) && !string.Equals(enemy.enemyName, enemy.id, System.StringComparison.Ordinal))
        {
            enemyDict[enemy.enemyName] = enemy;
        }
    }

    public static void Load()
    {
#if UNITY_ANDROID || UNITY_WEBGL
        Debug.LogError("EnemyDataDatabase.Load() is not supported on Android/WebGL. Use LoadAsync() and await it.");
#else
        LoadAsync().GetAwaiter().GetResult();
#endif
    }

    public static async Task EnsureLoadedAsync()
    {
        if (allEnemies != null && allEnemies.Count > 0)
            return;

        isLoaded = false;
        await LoadAsync();
    }

    public static EnemyData Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (enemyDict != null && enemyDict.TryGetValue(id, out var enemy))
            return enemy;

        if (enemyDict != null)
        {
            foreach (var pair in enemyDict)
            {
                if (string.Equals(pair.Key, id, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
        }

        return null;
    }
}