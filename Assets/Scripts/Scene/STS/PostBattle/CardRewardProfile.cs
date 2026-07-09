using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardRarityWeight
{
    public CardRarity rarity;
    public int weight = 1;

    public CardRarityWeight()
    {
    }

    public CardRarityWeight(CardRarity rarity, int weight)
    {
        this.rarity = rarity;
        this.weight = weight;
    }
}

[System.Serializable]
public class CardRewardProfile
{
    public bool useSelectedCharacterBias = true;
    public bool useSpecificFavoredCharacter = false;
    public SelectableCharacter specificFavoredCharacter = SelectableCharacter.Aucun;
    public bool allowCreatedCards = false;
    public bool allowUnobtainableCards = false;
    public bool useExactRarity = false;
    public CardRarity exactRarity = CardRarity.Common;
    public bool useExactCost = false;
    public int exactCost = 0;
    public bool useCostRange = false;
    public int minimumCost = 0;
    public int maximumCost = 0;
    public bool useExactType = false;
    public CardType exactType = CardType.Rien;
    public bool overrideDefaultWeights = false;
    public List<CardRarityWeight> rarityWeights = new();
    public List<CardTag> requiredTags = new();
    public List<CardTag> excludedTags = new();
    public int choiceCount = 3;

    public CardRewardProfile()
    {
    }

    public static CardRewardProfile ForAllCharacters(CombatResult result = null)
    {
        CardRewardProfile profile = CreateDefault(result);
        profile.useSelectedCharacterBias = false;
        profile.useSpecificFavoredCharacter = false;
        profile.specificFavoredCharacter = SelectableCharacter.Aucun;
        return profile;
    }

    public static CardRewardProfile ForCharacter(SelectableCharacter character, CombatResult result = null)
    {
        CardRewardProfile profile = CreateDefault(result);
        profile.useSelectedCharacterBias = false;
        profile.useSpecificFavoredCharacter = true;
        profile.specificFavoredCharacter = character;
        return profile;
    }

    public static CardRewardProfile ForRarity(CardRarity rarity, CombatResult result = null)
    {
        CardRewardProfile profile = ForAllCharacters(result);
        profile.useExactRarity = true;
        profile.exactRarity = rarity;
        return profile;
    }

    public static CardRewardProfile ForCharacterAndRarity(SelectableCharacter character, CardRarity rarity, CombatResult result = null)
    {
        CardRewardProfile profile = ForCharacter(character, result);
        profile.useExactRarity = true;
        profile.exactRarity = rarity;
        return profile;
    }

    public CardRewardProfile CreateRelaxedFallback()
    {
        CardRewardProfile profile = new CardRewardProfile(this);
        profile.useExactRarity = false;
        profile.useExactCost = false;
        profile.useCostRange = false;
        profile.useExactType = false;
        profile.requiredTags = new List<CardTag>();
        profile.excludedTags = new List<CardTag>();

        if (!profile.useSpecificFavoredCharacter)
        {
            profile.useSelectedCharacterBias = false;
            profile.specificFavoredCharacter = SelectableCharacter.Aucun;
        }

        return profile;
    }

    public CardRewardProfile(CardRewardProfile other)
    {
        if (other == null)
        {
            return;
        }

        useSelectedCharacterBias = other.useSelectedCharacterBias;
        useSpecificFavoredCharacter = other.useSpecificFavoredCharacter;
        specificFavoredCharacter = other.specificFavoredCharacter;
        allowCreatedCards = other.allowCreatedCards;
        allowUnobtainableCards = other.allowUnobtainableCards;
        useExactRarity = other.useExactRarity;
        exactRarity = other.exactRarity;
        useExactCost = other.useExactCost;
        exactCost = other.exactCost;
        useCostRange = other.useCostRange;
        minimumCost = other.minimumCost;
        maximumCost = other.maximumCost;
        useExactType = other.useExactType;
        exactType = other.exactType;
        overrideDefaultWeights = other.overrideDefaultWeights;
        choiceCount = other.choiceCount;

        rarityWeights = new List<CardRarityWeight>();
        if (other.rarityWeights != null)
        {
            foreach (var weight in other.rarityWeights)
            {
                if (weight != null)
                {
                    rarityWeights.Add(new CardRarityWeight(weight.rarity, weight.weight));
                }
            }
        }

        requiredTags = other.requiredTags != null ? new List<CardTag>(other.requiredTags) : new List<CardTag>();
        excludedTags = other.excludedTags != null ? new List<CardTag>(other.excludedTags) : new List<CardTag>();
    }

    public static CardRewardProfile CreateDefault(CombatResult result = null)
    {
        CardRewardProfile profile = new CardRewardProfile();

        if (result != null && result.boss)
        {
            profile.overrideDefaultWeights = true;
            profile.rarityWeights = new List<CardRarityWeight>
            {
                new CardRarityWeight(CardRarity.Common, 10),
                new CardRarityWeight(CardRarity.Uncommon, 25),
                new CardRarityWeight(CardRarity.Rare, 50),
                new CardRarityWeight(CardRarity.Epic, 75),
                new CardRarityWeight(CardRarity.Legendary, 100)
            };
        }

        return profile;
    }

    public bool Matches(STSCardData card, CombatResult result)
    {
        if (card == null||card.favoredCharacter == SelectableCharacter.Starting||card.favoredCharacter == SelectableCharacter.Impossible)
        {
            return false;
        }

        if (!allowCreatedCards && card.HasTag(CardTag.Created))
        {
            return false;
        }

        if (!allowUnobtainableCards && card.HasTag(CardTag.Unobtainable))
        {
            return false;
        }

        if (useSpecificFavoredCharacter && card.favoredCharacter != specificFavoredCharacter)
        {
            return false;
        }

        if (useSelectedCharacterBias && RunManager.Instance != null && RunManager.Instance.selectedCharacter != SelectableCharacter.Aucun && !useSpecificFavoredCharacter)
        {
            SelectableCharacter selectedCharacter = RunManager.Instance.selectedCharacter;

            if (card.favoredCharacter != SelectableCharacter.Aucun && card.favoredCharacter != selectedCharacter)
            {
                return false;
            }
        }

        if (useExactRarity && card.rarity != exactRarity)
        {
            return false;
        }

        if (useExactCost && card.cost != exactCost)
        {
            return false;
        }

        if (useCostRange && (card.cost < minimumCost || card.cost > maximumCost))
        {
            return false;
        }

        if (useExactType && card.type != exactType)
        {
            return false;
        }

        if (requiredTags != null)
        {
            foreach (CardTag tag in requiredTags)
            {
                if (!card.HasTag(tag))
                {
                    return false;
                }
            }
        }

        if (excludedTags != null)
        {
            foreach (CardTag tag in excludedTags)
            {
                if (card.HasTag(tag))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int GetWeight(CardRarity rarity, CombatResult result)
    {
        if (rarityWeights != null && rarityWeights.Count > 0)
        {
            CardRarityWeight customWeight = rarityWeights.Find(weight => weight.rarity == rarity);

            if (customWeight != null)
            {
                return Mathf.Max(0, customWeight.weight);
            }

            if (overrideDefaultWeights)
            {
                return 0;
            }
        }

        return GetDefaultWeight(rarity, result);
    }

    public static int GetDefaultWeight(CardRarity rarity, CombatResult result)
    {
        if (result != null && result.boss)
        {
            return rarity switch
            {
                CardRarity.Common => 10,
                CardRarity.Uncommon => 25,
                CardRarity.Rare => 50,
                CardRarity.Epic => 75,
                CardRarity.Legendary => 100,
                _ => 0
            };
        }
        if (result.act<=0)
        {
            return rarity switch
            {
                CardRarity.Common => 75,
                CardRarity.Uncommon => 50,
                CardRarity.Rare => 25,
                CardRarity.Epic => 10,
                CardRarity.Legendary => 5,
                _ => 0
            };
        }
        if (result.act==1)
        {
            return rarity switch
            {
                CardRarity.Common => 50,
                CardRarity.Uncommon => 50,
                CardRarity.Rare => 30,
                CardRarity.Epic => 15,
                CardRarity.Legendary => 5,
                _ => 0
            };
        }
        if (result.act==2)
        {
            return rarity switch
            {
                CardRarity.Common => 30,
                CardRarity.Uncommon => 30,
                CardRarity.Rare => 20,
                CardRarity.Epic => 10,
                CardRarity.Legendary => 5,
                _ => 0
            };
        }
        return rarity switch
        {
            CardRarity.Common => 20,
            CardRarity.Uncommon => 20,
            CardRarity.Rare => 15,
            CardRarity.Epic => 10,
            CardRarity.Legendary => 5,
            _ => 0
        };
    }

    public string GetShortDescription()
    {
        List<string> parts = new List<string>();

        if (useSpecificFavoredCharacter)
        {
            parts.Add("pour " + specificFavoredCharacter);
        }
        else if (!useSelectedCharacterBias)
        {
            parts.Add("pour toutes les classes");
        }

        if (useExactRarity)
        {
            parts.Add(GetRarityLabel(exactRarity));
        }

        if (useExactCost)
        {
            parts.Add(exactCost + "-coût");
        }
        else if (useCostRange)
        {
            parts.Add("coût " + minimumCost + "-" + maximumCost);
        }

        if (useExactType)
        {
            parts.Add(exactType.ToString().ToLowerInvariant());
        }

        if (requiredTags != null && requiredTags.Count > 0)
        {
            parts.Add("tag " + requiredTags[0]);
        }

        if (parts.Count == 0)
        {
            return "Recevez une carte";
        }

        return "Recevez une carte " + string.Join(", ", parts);
    }

    private static string GetRarityLabel(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => "commune",
            CardRarity.Uncommon => "inhabituelle",
            CardRarity.Rare => "rare",
            CardRarity.Epic => "épique",
            CardRarity.Legendary => "légendaire",
            _ => rarity.ToString().ToLowerInvariant()
        };
    }
}