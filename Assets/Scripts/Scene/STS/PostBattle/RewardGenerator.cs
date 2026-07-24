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
        result ??= new CombatResult();

        Reward reward = new Reward();

        CardRewardProfile cardRewardProfile = CardRewardProfile.CreateDefault(result);
        if (result.boss)
        {
            cardRewardProfile.useExactRarity = true;
            cardRewardProfile.exactRarity = CardRarity.Legendary;
            cardRewardProfile.choiceCount = 3;
        }

        CardReward cardReward = new CardReward
        {
            choices = GenerateCardChoices(result, cardRewardProfile)
        };

        Relic relic = null;
        do{
            relic = RelicDrop.GetRandomRelic(result);
        } while (relic==null||(RunManager.Instance!=null&&RunManager.Instance.relics.Exists(r=>r.name==relic.name)));
        if (relic != null && (result.elite || result.boss))
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
        if (result.boss&&RunManager.Instance!=null&&RunManager.Instance.act%3==2)
        {
            BaseRelicUpgradeReward upgradeReward = GenerateRelicUpgradeReward(result);
            if (upgradeReward != null) reward.items.Add(upgradeReward);
        }
        reward.items.Add(cardReward);
        return reward;
    }
    public static CardReward GenerateCardReward(CombatResult result = null)
    {
        result ??= new CombatResult();

        return GenerateCardReward(result, null);
    }

    public static CardReward GenerateCardReward(CombatResult result, CardRewardProfile profile)
    {
        result ??= new CombatResult();
        profile ??= CardRewardProfile.CreateDefault(result);

        return new CardReward
        {
            choices = GenerateCardChoices(result, profile)
        };
    }
    public static RelicReward GenerateRelicReward(CombatResult result = null)
    {
        result ??= new CombatResult();
        Relic relic=null;
        do{
            relic = RelicDrop.GetRandomRelic(result);
        } while(relic==null||(RunManager.Instance!=null&&RunManager.Instance.relics.Exists(r=>r.name==relic.name)));
        return new RelicReward
        {
            relic = relic
        };
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
    static List<CardInstance> GenerateCardChoices(CombatResult result, CardRewardProfile profile)
    {
        result ??= new CombatResult();
        profile ??= CardRewardProfile.CreateDefault(result);

        List<CardInstance> choices = GenerateCardChoicesFromPool(result, profile, BuildCardPool(result, profile));

        if (choices.Count > 0)
        {
            return choices;
        }

        CardRewardProfile relaxedProfile = profile.CreateRelaxedFallback();
        Debug.LogWarning("Card reward profile produced no choices. Retrying with a relaxed fallback profile.");

        choices = GenerateCardChoicesFromPool(result, relaxedProfile, BuildCardPool(result, relaxedProfile));

        if (choices.Count > 0)
        {
            return choices;
        }

        Debug.LogWarning("Relaxed card reward fallback also produced no choices. Retrying with the default combat pool.");
        return GenerateCardChoicesFromPool(result, CardRewardProfile.CreateDefault(result), BuildCardPool(result, CardRewardProfile.CreateDefault(result)));
    }

    static List<CardInstance> GenerateCardChoicesFromPool(CombatResult result, CardRewardProfile profile, List<CardEntry> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            return new List<CardInstance>();
        }

        List<CardInstance> choices = new List<CardInstance>();
        int maxAttempts = Mathf.Max(20, profile.choiceCount * 50);
        HashSet<string> seenCardIds = new HashSet<string>();

        for (int i = 0; i < profile.choiceCount; i++)
        {
            CardInstance card = null;
            int attempts = 0;

            do
            {
                card = GetRandomCard(pool, result);

                if (card == null)
                {
                    break;
                }

                attempts++;
            } while (card != null && card.data != null && seenCardIds.Contains(card.data.id) && attempts < maxAttempts);

            if (card == null)
            {
                break;
            }

            if (card.data != null)
            {
                seenCardIds.Add(card.data.id);
            }

            if (result != null && result.elite && card.enchantments != null && card.enchantments.Count == 0)
            {
                EnchantManager.ApplyEnchant(card, 1, includeTreasureEnchants: true);
            }

            choices.Add(card);
        }

        return choices;
    }
    static List<CardEntry> BuildCardPool(CombatResult result, CardRewardProfile profile)
    {
        List<CardEntry> pool = new List<CardEntry>();

        if (result.enemies != null&&RunManager.Instance!=null && RunManager.Instance.relics.Exists(r=>r is ITIRelic))
        {
            ITIRelic iRelic = RunManager.Instance.relics.Find(r => r is ITIRelic) as ITIRelic;
            Debug.Log("Adding enemy reward cards to pool");
            foreach (var enemy in result.enemies)
            {
                foreach (var card in enemy.rewardCards)
                {
                    if (profile.Matches(card, result))
                    {
                        pool.Add(new CardEntry(card, iRelic.DropRateForEnemyCards()));
                    }
                }
            }
        }

        pool.AddRange(GetFloorCards(RunManager.Instance!=null ? RunManager.Instance.currentFloor : 1,result,profile));

        return pool;
    }
    static CardInstance GetRandomCard(List<CardEntry> pool,CombatResult result=null)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;
        foreach (var entry in pool)
        {
            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (var entry in pool)
        {
            cumulativeWeight += Mathf.Max(0, entry.weight);
            if (randomValue < cumulativeWeight)
            {
                CardInstance cardInstance = new CardInstance(entry.card);
                if (result != null&&result.elite)
                {
                    Debug.Log("Enchanting card for elite reward");
                    EnchantManager.ApplyEnchant(cardInstance,Random.Range(1, 5+(RunManager.Instance!=null?RunManager.Instance.act*2:1)));
                }
                return cardInstance;
            }
        }

        return null; // Should never reach here if pool is not empty
    }
    static List<CardEntry> GetFloorCards(int floor,CombatResult result=null, CardRewardProfile profile = null)
    {
        List<CardEntry> floorCards = new List<CardEntry>();
        if (STSCardDatabase.allCards==null || STSCardDatabase.allCards.Count==0)
        {
            STSCardDatabase.EnsureLoadedAsync().GetAwaiter().GetResult();
        }
        foreach (var card in STSCardDatabase.allCards)
        {
            if (profile != null && !profile.Matches(card, result))
            {
                continue; // Skip cards that do not match the requested profile
            }
            int weight;
            if (profile != null)
            {
                weight = profile.GetWeight(card.rarity, result);
            }
            else
            {
                weight = CardRewardProfile.GetDefaultWeight(card.rarity, result);
            }

            if (weight <= 0)
            {
                continue;
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