using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class PullManager : MonoBehaviour
{
    public static PullManager Instance;
    public CardData[][] possiblePulls;
    public PackData ChosenPack;
    public int highestRarity;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GeneratePull(PackData packData)
    {
        possiblePulls = new CardData[Random.Range(5, 8)][];
        for(int i=0; i<possiblePulls.Length;i++)
            possiblePulls[i] = GetPulledCards(packData);
        highestRarity = 0;
        foreach (var pull in possiblePulls)
        {
            foreach (var card in pull)
            if (card != null)
            {
            if (card.rarity > highestRarity)
                highestRarity = card.rarity;
            }
        }
    }
    
    private CardData[] GetPulledCards(PackData packData)
    {
        CardData[] result= new CardData[packData.cardCount];
        for (int i = 0; i < packData.cardCount; i++)
        {
            // Sélectionner une carte aléatoire parmi les cartes possibles du pack
            float totalWeight = 0f;
            foreach (var entry in packData.possibleCards)
            {
                totalWeight += entry.weight;
            }
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            foreach (var entry in packData.possibleCards)
            {
                cumulativeWeight += entry.weight;
                if (randomValue <= cumulativeWeight)
                {
                    var cardData = CardDatabase.Instance.cards
                        .FirstOrDefault(c => c.cardId == entry.cardId);
                    result[i] = cardData;
                    if (cardData == null)
                    {
                        Debug.LogError("[PullManager] CardData not found for cardId: " + entry.cardId);
                    }
                    break;
                }
            }
        }
        return result;
    }
}