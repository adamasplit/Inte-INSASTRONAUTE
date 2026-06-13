using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class StreamingAssetsLoader
{
    [Serializable]
    private class FileListWrapper
    {
        public List<string> files = new List<string>();
    }

    public static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalized = path.Replace("\\", "/");
        const string streamingPrefix = "Assets/StreamingAssets/";

        if (normalized.StartsWith(streamingPrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(streamingPrefix.Length);
        }

        string streamingRoot = Application.streamingAssetsPath.Replace("\\", "/");
        if (normalized.StartsWith(streamingRoot, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(streamingRoot.Length);
        }

        return normalized.TrimStart('/');
    }

    public static string GetStreamingAssetsRoot()
    {
        string streamingAssetsPath = Application.streamingAssetsPath;
        if (Path.IsPathRooted(streamingAssetsPath))
        {
            return Path.GetFullPath(streamingAssetsPath);
        }

        string normalized = streamingAssetsPath.Replace("\\", "/");
        if (normalized.Equals("Assets/StreamingAssets", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("Assets/StreamingAssets/", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "StreamingAssets"));
        }

        return Path.GetFullPath(Path.Combine(Application.dataPath, normalized));
    }

    public static async Task<string> ReadAllTextAsync(string path)
    {
        string relativePath = NormalizeRelativePath(path);

#if UNITY_ANDROID || UNITY_WEBGL
        string url = BuildStreamingAssetUrl(relativePath);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isHttpError || request.isNetworkError)
#endif
            {
                Debug.LogError($"Failed to read StreamingAssets file '{relativePath}' from '{url}': {request.error}");
                return null;
            }

            return request.downloadHandler.text;
        }
#else
        string fullPath = Path.GetFullPath(Path.Combine(GetStreamingAssetsRoot(), relativePath));
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"StreamingAssets file not found: {fullPath}");
            return null;
        }

        return File.ReadAllText(fullPath);
#endif
    }

    public static async Task<List<string>> ListJsonFilesAsync(string folderPath)
    {
        string normalizedFolder = NormalizeRelativePath(folderPath).TrimEnd('/');

#if UNITY_ANDROID || UNITY_WEBGL
        string indexPath = string.IsNullOrEmpty(normalizedFolder)
            ? "index.json"
            : normalizedFolder + "/index.json";

        string indexJson = await ReadAllTextAsync(indexPath);
        if (string.IsNullOrEmpty(indexJson))
        {
            Debug.LogError($"Missing or unreadable index file for StreamingAssets folder '{normalizedFolder}'. Expected '{indexPath}'.");
            return new List<string>();
        }

        FileListWrapper wrapper = JsonUtility.FromJson<FileListWrapper>(indexJson);
        if (wrapper == null || wrapper.files == null)
        {
            Debug.LogError($"Invalid index JSON format at '{indexPath}'.");
            return new List<string>();
        }

        List<string> relativeFiles = new List<string>();
        for (int i = 0; i < wrapper.files.Count; i++)
        {
            string entry = wrapper.files[i];
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            string normalizedEntry = entry.Replace("\\", "/").TrimStart('/');
            if (!normalizedEntry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(normalizedFolder)
                && !normalizedEntry.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedEntry = normalizedFolder + "/" + normalizedEntry;
            }

            relativeFiles.Add(normalizedEntry);
        }

        return relativeFiles;
#else
        string fullFolderPath = Path.GetFullPath(Path.Combine(GetStreamingAssetsRoot(), normalizedFolder));
        if (!Directory.Exists(fullFolderPath))
        {
            Debug.LogError($"StreamingAssets directory not found: {fullFolderPath}");
            return new List<string>();
        }

        string[] files = Directory.GetFiles(fullFolderPath, "*.json");
        Debug.Log($"StreamingAssets folder '{fullFolderPath}' has {files.Length} json files.");
        List<string> relativeFiles = new List<string>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            if (string.Equals(fileName, "index.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            relativeFiles.Add(string.IsNullOrEmpty(normalizedFolder) ? fileName : normalizedFolder + "/" + fileName);
        }

        return relativeFiles;
#endif
    }

    private static string BuildStreamingAssetUrl(string relativePath)
    {
        string root = Application.streamingAssetsPath;
        if (root.EndsWith("/", StringComparison.Ordinal))
        {
            return root + relativePath;
        }

        return root + "/" + relativePath;
    }
}