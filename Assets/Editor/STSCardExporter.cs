using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

public static class STSCardExporter
{
    [MenuItem("Tools/Export STS Cards")]
    public static void ExportCards()
    {
        string folder = "Assets/StreamingAssets/STSCardData";

        var guids = AssetDatabase.FindAssets("t:STSCardData");

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            STSCardData card =
                AssetDatabase.LoadAssetAtPath<STSCardData>(path);

            STSCardDataDTO dto = card.ToDTO();

            string json =
                JsonConvert.SerializeObject(dto, Formatting.Indented);

            string outputPath =
                Path.Combine(folder, dto.id + ".json");

            File.WriteAllText(outputPath, json);
        }

        AssetDatabase.Refresh();

        Debug.Log("Cards exported.");
    }
}