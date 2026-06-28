using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

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
                            continue;
                        }

                        EnemyData enemy = EnemyData.FromDTO(dto);
                        RegisterEnemy(enemy);
                        allEnemies.Add(enemy);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load combined enemies.json: {ex}");
            }
        }
        else
        {
            List<string> files = await StreamingAssetsLoader.ListJsonFilesAsync("EnemyData");
            Debug.Log($"EnemyDataDatabase found {files.Count} enemy JSON files.");

            foreach (string file in files)
            {
                try
                {
                    string json = await StreamingAssetsLoader.ReadAllTextAsync(file);
                    if (string.IsNullOrEmpty(json))
                        continue;

                    EnemyDataDTO dto = JsonConvert.DeserializeObject<EnemyDataDTO>(json);

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
        }

        isLoaded = allEnemies.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("EnemyDataDatabase loaded zero enemies. Check StreamingAssets/EnemyData and its JSON contents.");
            enemyDict = null;
            allEnemies = null;
        }

        loadTask = null;
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
        if (enemyDict != null && enemyDict.TryGetValue(id, out var enemy))
            return enemy;

        return null;
    }
}