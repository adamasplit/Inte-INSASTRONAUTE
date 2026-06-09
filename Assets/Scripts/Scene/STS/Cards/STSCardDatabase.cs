using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class STSCardDatabase
{
    static Dictionary<string, STSCardData> cardDict;

    public static List<STSCardData> allCards;

    public static void Load()
    {
        cardDict = new();
        allCards = new();

        string path =
            Path.Combine(Application.streamingAssetsPath, "STSCardData");

        string[] files = Directory.GetFiles(path, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);

            STSCardDataDTO dto =
                JsonConvert.DeserializeObject<STSCardDataDTO>(json);

            STSCardData card =
                STSCardData.FromDTO(dto);

            cardDict[card.cardName] = card;

            allCards.Add(card);
        }
    }

    public static STSCardData Get(string id)
    {
        if (cardDict.TryGetValue(id, out var card))
            return card;

        Debug.LogError($"Card {id} not found!");

        return null;
    }
    public static List<STSCardData> CardForCollectionCard(string collectionCardId)
    {
        List<STSCardData> cards = new List<STSCardData>();

        foreach (var card in allCards)
        {
            if (card.collectionCardId == collectionCardId)
            {
                cards.Add(card);
            }
        }

        return cards;
    }
}