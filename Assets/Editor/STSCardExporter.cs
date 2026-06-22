using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
public static class STSCardExporter
{
    [MenuItem("Tools/Export STS Cards")]
    public static void ExportCards()
    {
        string folder = Path.Combine(StreamingAssetsLoader.GetStreamingAssetsRoot(), "STSCardData");
        Directory.CreateDirectory(folder);

        var guids = AssetDatabase.FindAssets("t:STSCardData");
        List<STSCardDataDTO> cardDtos = new();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            STSCardData card =
                AssetDatabase.LoadAssetAtPath<STSCardData>(path);

            STSCardDataDTO dto = card.ToDTO();

            string json =
                JsonConvert.SerializeObject(dto, Formatting.Indented);

            string outputPath = Path.Combine(folder, dto.id + ".json");

            File.WriteAllText(outputPath, json);
            cardDtos.Add(dto);
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

        string cardsJson = JsonConvert.SerializeObject(new CardDatabaseWrapper(cardDtos), Formatting.Indented);
        File.WriteAllText(Path.Combine(folder, "cards.json"), cardsJson);

        AssetDatabase.Refresh();

        Debug.Log("Cards exported.");
        // Also debug log the number of cards exported for a given favored character, for example "Exported 10 cards for character: Warrior"
        Dictionary<SelectableCharacter, int> characterCardCounts = new();
        foreach (var dto in cardDtos)
        {
            if (!characterCardCounts.ContainsKey(Enum.Parse<SelectableCharacter>(dto.favoredCharacter.ToString())))
            {
                characterCardCounts[Enum.Parse<SelectableCharacter>(dto.favoredCharacter.ToString())] = 0;
            }
            if (!dto.tags.Contains("Created"))
            {
                characterCardCounts[Enum.Parse<SelectableCharacter>(dto.favoredCharacter.ToString())]++;
            }
        }

        foreach (var kvp in characterCardCounts)
        {
            Debug.Log($"Exported {kvp.Value} cards for character: {kvp.Key}");
        }
    }

    [System.Serializable]
    private class CardDatabaseWrapper
    {
        public List<STSCardDataDTO> cards;

        public CardDatabaseWrapper(List<STSCardDataDTO> cards)
        {
            this.cards = cards;
        }
    }
}