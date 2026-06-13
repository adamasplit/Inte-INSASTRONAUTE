using System.Collections.Generic;
using UnityEngine;
public static class RewardGenerator
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
    public static Reward GenerateReward(CombatResult result)
    {
        Reward reward = new Reward();

        CardReward cardReward = new CardReward
        {
            choices = GenerateCardChoices(result)
        };

        

        Relic relic = RelicDrop.GetRandomRelic(result);
        if (relic != null&&result.elite)
        {
            reward.items.Add(new RelicReward
            {
                relic = relic
            });
        }
        reward.items.Add(new GoldReward
        {
            amount = GenerateGold(result)
        });
        if (result.boss)
        {
            BaseRelicUpgradeReward upgradeReward = GenerateRelicUpgradeReward(result);
            if (upgradeReward != null) reward.items.Add(upgradeReward);
        }
        reward.items.Add(cardReward);
        return reward;
    }
    static int GenerateGold(CombatResult result)
    {
        int baseGold = Random.Range(15, 26);

        if (result.elite)
            baseGold += 25;

        if (result.boss)
            baseGold += 100;

        return baseGold;
    }
    static List<CardInstance> GenerateCardChoices(CombatResult result)
    {
        List<CardEntry> pool = BuildCardPool(result);

        List<CardInstance> choices = new List<CardInstance>();

        for (int i = 0; i < 3; i++)
        {
            choices.Add(GetRandomCard(pool,result));
        }

        return choices;
    }
    static List<CardEntry> BuildCardPool(CombatResult result)
    {
        List<CardEntry> pool = new List<CardEntry>();

        if (result.enemies != null&&RunManager.Instance!=null && RunManager.Instance.relics.Exists(r => r is ITIRelic))
        {
            ITIRelic iRelic = RunManager.Instance.relics.Find(r => r is ITIRelic) as ITIRelic;
            Debug.Log("Adding enemy reward cards to pool");
            foreach (var enemy in result.enemies)
            {
                foreach (var card in enemy.rewardCards)
                {
                    pool.Add(new CardEntry(card, iRelic.DropRateForEnemyCards()));
                }
            }
        }

        pool.AddRange(GetFloorCards(RunManager.Instance!=null ? RunManager.Instance.currentFloor : 1,result));

        return pool;
    }
    static CardInstance GetRandomCard(List<CardEntry> pool,CombatResult result=null)
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
                CardInstance cardInstance = new CardInstance(entry.card);
                if (result != null&&result.elite)
                {
                    Debug.Log("Enchanting card for elite reward");
                    EnchantManager.ApplyEnchant(cardInstance,Random.Range(1, 15));
                }
                return cardInstance;
            }
        }

        return null; // Should never reach here if pool is not empty
    }
    static List<CardEntry> GetFloorCards(int floor,CombatResult result=null)
    {
        List<CardEntry> floorCards = new List<CardEntry>();
        if (STSCardDatabase.allCards==null || STSCardDatabase.allCards.Count==0)
        {
            STSCardDatabase.EnsureLoadedAsync().GetAwaiter().GetResult();
        }
        foreach (var card in STSCardDatabase.allCards)
        {
            if (card.favoredCharacter != SelectableCharacter.Aucun && RunManager.Instance != null 
            && card.favoredCharacter != RunManager.Instance.selectedCharacter
            ||card.created)
            {
                continue; // Skip cards that are favored for a different character or created cards
            }
            int weight;
            if (result.boss)
            {
                weight=card.rarity switch
                {
                    CardRarity.Legendary => 50,
                    _ => 0
                };
            }
            else
            {
                weight=card.rarity switch
                {
                    CardRarity.Common => 100,
                    CardRarity.Uncommon => 50,
                    CardRarity.Rare => 25,
                    CardRarity.Epic => 10,
                    CardRarity.Legendary => 5,
                    _ => 0
                };
            }
            floorCards.Add(new CardEntry(card, weight));
        }
        return floorCards;
    }
    static BaseRelicUpgradeReward GenerateRelicUpgradeReward(CombatResult result)
    {
        if (RunManager.Instance == null) return null;

        List<BaseRelic> upgradableRelics = RunManager.Instance.relics.FindAll(r => r is BaseRelic).ConvertAll(r => r as BaseRelic);

        if (upgradableRelics.Count == 0) return null;

        BaseRelic relicToUpgrade = upgradableRelics[Random.Range(0, upgradableRelics.Count)];

        int upgradeStage = relicToUpgrade.GetUpgradeStage();

        if (relicToUpgrade.descriptionsByStage[upgradeStage]=="")
        {
            return null; // No further upgrade available for this relic
        }

        return new BaseRelicUpgradeReward
        {
            relic = relicToUpgrade,
            stage = upgradeStage
        };
    }
}