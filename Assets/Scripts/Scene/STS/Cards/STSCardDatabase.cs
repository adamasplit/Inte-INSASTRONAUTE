using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public static class STSCardDatabase
{
    static Dictionary<string, STSCardData> cardDict;
    public static List<STSCardData> allCards;
    public static void Load()
    {
        cardDict = new Dictionary<string, STSCardData>();
        var cards = Resources.LoadAll<STSCardData>("STS/Cards");
        foreach (var card in cards)
        {
            cardDict[card.cardName] = card;
        }
        allCards = new List<STSCardData>(cardDict.Values);
    }
    public static STSCardData Get(string name)
    {
        if (cardDict.TryGetValue(name, out var card))
            return card;
        Debug.LogError($"Card {name} not found in database!");
        return null;
    }
}