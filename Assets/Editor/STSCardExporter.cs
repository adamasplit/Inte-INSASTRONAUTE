using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
public static class STSCardExporter
{
    [MenuItem("Tools/Export STS Cards")]
    public static void ExportCards()
    {
        string folder = Path.Combine(StreamingAssetsLoader.GetStreamingAssetsRoot(), "STSCardData");
        Directory.CreateDirectory(folder);

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

        List<string> cardFiles = new();
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            STSCardData card =
                AssetDatabase.LoadAssetAtPath<STSCardData>(path);
            if (card != null)
            {
                cardFiles.Add(card.id + ".json");
            }
        }

        cardFiles.Sort(System.StringComparer.OrdinalIgnoreCase);

        string indexJson = JsonConvert.SerializeObject(new { files = cardFiles }, Formatting.Indented);
        File.WriteAllText(Path.Combine(folder, "index.json"), indexJson);

        AssetDatabase.Refresh();

        Debug.Log("Cards exported.");
    }
}