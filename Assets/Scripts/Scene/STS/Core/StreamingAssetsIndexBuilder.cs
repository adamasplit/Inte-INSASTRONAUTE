using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class StreamingAssetsIndexBuilder
{
    [Serializable]
    private class FileListWrapper
    {
        public List<string> files;

        public FileListWrapper(List<string> files)
        {
            this.files = files;
        }
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Rebuild StreamingAssets JSON Indexes")]
    public static void RebuildIndexes()
    {
        BuildFolderIndex("STSCardData");
        BuildFolderIndex("EnemyData");
        BuildFolderIndex("Players");
        AssetDatabase.Refresh();
        Debug.Log("StreamingAssets JSON indexes rebuilt.");
    }

    private static void BuildFolderIndex(string folderName)
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, folderName);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"StreamingAssets folder not found, skipping index build: {folderPath}");
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.json");
        List<string> fileNames = new List<string>();
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            if (string.Equals(fileName, "index.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            fileNames.Add(fileName);
        }

        fileNames.Sort(StringComparer.OrdinalIgnoreCase);

        string indexPath = Path.Combine(folderPath, "index.json");
        string json = JsonUtility.ToJson(new FileListWrapper(fileNames), true);
        File.WriteAllText(indexPath, json);

        Debug.Log($"Wrote {fileNames.Count} entries to {indexPath}");
    }
#endif
}
