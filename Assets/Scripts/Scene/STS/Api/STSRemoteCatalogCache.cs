using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Persists catalog responses by immutable game-data release version. A short version request is
/// made on each application launch; matching releases are read locally instead of downloading all
/// catalog JSON again.
/// </summary>
public static class STSRemoteCatalogCache
{
    [Serializable]
    private class BridgeVersionResponse
    {
        public bool ok;
        public VersionData data;
    }

    [Serializable]
    private class VersionData
    {
        public string dataVersion;
    }

    private const string CacheFolderName = "sts-catalog-cache";
    private static Task<string> versionTask;

    public static async Task<string> GetOrFetchAsync(string catalogName, Func<Task<string>> fetchRemote)
    {
        if (fetchRemote == null)
            throw new ArgumentNullException(nameof(fetchRemote));

        string version = await GetVersionAsync();
        if (!string.IsNullOrWhiteSpace(version))
        {
            string cachedJson = ReadCachedCatalog(version, catalogName);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                Debug.Log($"STS catalog cache hit: {catalogName} ({version}).");
                return cachedJson;
            }
        }

        string remoteJson = await fetchRemote();
        if (!string.IsNullOrWhiteSpace(remoteJson) && !string.IsNullOrWhiteSpace(version))
        {
            WriteCachedCatalog(version, catalogName, remoteJson);
        }

        return remoteJson;
    }

    private static async Task<string> GetVersionAsync()
    {
        if (versionTask == null)
        {
            versionTask = GetVersionInternalAsync();
        }

        return await versionTask;
    }

    private static async Task<string> GetVersionInternalAsync()
    {
        string json = await ReactApiBridge.RequestStsCatalogVersionAsync();
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            BridgeVersionResponse response = JsonConvert.DeserializeObject<BridgeVersionResponse>(json);
            return response != null && response.ok ? response.data?.dataVersion : null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"STS catalog cache could not parse the release version: {ex.Message}");
            return null;
        }
    }

    private static string ReadCachedCatalog(string version, string catalogName)
    {
        try
        {
            string path = CachePath(version, catalogName);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"STS catalog cache could not read '{catalogName}': {ex.Message}");
            return null;
        }
    }

    private static void WriteCachedCatalog(string version, string catalogName, string json)
    {
        try
        {
            string path = CachePath(version, catalogName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            Debug.Log($"STS catalog cache stored: {catalogName} ({version}).");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"STS catalog cache could not store '{catalogName}': {ex.Message}");
        }
    }

    private static string CachePath(string version, string catalogName)
    {
        string safeVersion = Uri.EscapeDataString(version);
        string safeCatalogName = Uri.EscapeDataString(catalogName);
        return Path.Combine(Application.persistentDataPath, CacheFolderName, safeVersion, safeCatalogName + ".json");
    }
}