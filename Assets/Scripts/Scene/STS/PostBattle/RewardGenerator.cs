using System.Collections.Generic;
using UnityEngine;
public class RewardGenerator
{
    public class CardEntry
    {
        public STSCardData card;
        public int weight;

        public CardEntry(STSCardData card, int weight)
        {
            this.card = card;
            this.weight = weight;
        }
    }
    public List<STSCardData> GenerateCardChoices(CombatResult result)
    {
        List<CardEntry> pool = BuildCardPool(result);

        List<STSCardData> choices = new List<STSCardData>();

        for (int i = 0; i < 3; i++)
        {
            choices.Add(GetRandomCard(pool));
        }

        return choices;
    }
    List<CardEntry> BuildCardPool(CombatResult result)
    {
        List<CardEntry> pool = new List<CardEntry>();

        if (result.enemies != null&&RunManager.Instance!=null && RunManager.Instance.relics.Exists(r => r is ITIRelic))
        {
            Debug.Log("Adding enemy reward cards to pool");
            foreach (var enemy in result.enemies)
            {
                foreach (var card in enemy.rewardCards)
                {
                    pool.Add(new CardEntry(card, 100));
                }
            }
        }

        pool.AddRange(GetFloorCards(RunManager.Instance!=null ? RunManager.Instance.currentFloor : 1));

        return pool;
    }
    public STSCardData GetRandomCard(List<CardEntry> pool)
    {
        int totalWeight = 0;
        foreach (var entry in pool)
        {
            totalWeight += entry.weight;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (var entry in pool)
        {
            cumulativeWeight += entry.weight;
            if (randomValue < cumulativeWeight)
            {
                return entry.card;
            }
        }

        return null; // Should never reach here if pool is not empty
    }
    List<CardEntry> GetFloorCards(int floor)
    {
        List<CardEntry> floorCards = new List<CardEntry>();
        if (STSCardDatabase.allCards==null)
        {
            STSCardDatabase.Load();
        }
        foreach (var card in STSCardDatabase.allCards)
        {
            int weight = card.rarity switch
            {
                CardRarity.Common => 100,
                CardRarity.Uncommon => 50,
                CardRarity.Rare => 25,
                CardRarity.Epic => 10,
                CardRarity.Legendary => 5,
                _ => 0
            };
            floorCards.Add(new CardEntry(card, weight));
        }
        return floorCards;
    }
}