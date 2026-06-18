using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class StreamingAssetsIndexPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        try
        {
            RebuildIndexes();
            Debug.Log("StreamingAssets index files rebuilt before build.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to rebuild StreamingAssets indexes: {ex}");
        }
    }

    private static void RebuildIndexes()
    {
        STSCardExporter.ExportCards();
        BuildFolderIndex("STSCardData");
        BuildFolderIndex("Players");
        BuildFolderIndex("Events");
        AssetDatabase.Refresh();
    }

    private static void BuildFolderIndex(string folderName)
    {
        string folderPath = Path.Combine(StreamingAssetsLoader.GetStreamingAssetsRoot(), folderName);
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
    }

    [Serializable]
    private class FileListWrapper
    {
        public List<string> files;

        public FileListWrapper(List<string> files)
        {
            this.files = files;
        }
    }
}
