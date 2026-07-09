using System;
using UnityEngine;
using System.Collections.Generic;
[Serializable]
public class EffectEntry
{
    public EffectType type;
    public int value;
    public bool targetSelf=false;
    public bool targetOthers=false; // Effects that target other enemies besides the target
    public StatusType statusType;
    public int duration;
    public string description; // Optional custom description for the effect
    public string cardID; // Optional: ID of the card this effect will create (for AddCardToHand or similar effects)
    public int index; // Optional: Index of the effect for status effects that have multiple effects (like FieldTurnFollowUp)
    public bool conditional;
    public ConditionType conditionType;
    public string conditionValue;
    public bool trueEffect; // For effects that have a "true" version that ignores dispel (like StealBuff vs TrueStealBuff)
    public CardSelectionSource cardSelectionSource; // For effects that involve selecting cards, specify the source of the cards
    public List<CardFilterTag> cardFilterTags = new(); // For effects that involve selecting cards, specify tags to filter the cards
    public CardSelectionEffect cardSelectionEffect; // For effects that involve selecting cards, specify an additional effect to apply to the selected cards
    public AnimationType animationType=AnimationType.Default; // For effects that have a specific animation type, specify it here
    public EffectEntryDTO ToDTO()
    {
        List<string> cft = new();
        foreach (var tag in cardFilterTags)
        {
            cft.Add(tag.ToString());
        }
        return new EffectEntryDTO
        {
            type = type.ToString(),

            value = value,

            targetSelf = targetSelf,

            statusType = statusType.ToString(),

            duration = duration,
            targetOthers = targetOthers,
            description = description,
            cardID = cardID,
            conditional = conditional,
            conditionType = conditionType.ToString(),
            conditionValue = conditionValue,
            trueEffect = trueEffect,
            cardSelectionSource = cardSelectionSource.ToString(),
            cardFilterTags = cft,
            index = index,
            cardSelectionEffect = cardSelectionEffect.ToString(),
            animationType = animationType.ToString()
        };
    }
    public static EffectEntry FromDTO(EffectEntryDTO dto)
    {
        List<CardFilterTag> cft = new();
        foreach (var tag in dto.cardFilterTags)        {
            cft.Add(Enum.Parse<CardFilterTag>(tag));
        }
        return new EffectEntry
        {
            type = Enum.Parse<EffectType>(dto.type),

            value = dto.value,

            targetSelf = dto.targetSelf,

            statusType = Enum.Parse<StatusType>(dto.statusType),
            targetOthers = dto.targetOthers,
            duration = dto.duration,

            description = dto.description,
            cardID = dto.cardID,
            conditional = dto.conditional,
            conditionType = Enum.Parse<ConditionType>(dto.conditionType),
            conditionValue = dto.conditionValue,
            trueEffect = dto.trueEffect,
            cardSelectionSource = Enum.Parse<CardSelectionSource>(dto.cardSelectionSource),
            cardFilterTags = cft,
            index = dto.index,
            cardSelectionEffect = Enum.Parse<CardSelectionEffect>(dto.cardSelectionEffect),
            animationType = Enum.Parse<AnimationType>(dto.animationType)
        };
    }
    public string GetEffectName()
    {
        return type switch
        {
            EffectType.Damage => "Damage"+animationType.ToString(),
            EffectType.Heal => "Heal",
            EffectType.Armor => "Armor",
            EffectType.Status=>StatusEffect.Factory(statusType,value,duration,cardID,index).debuff?"Debuff":"Buff",
            EffectType.AdvanceTurn=>"TurnAdvance",
            EffectType.DelayTurn=>"TurnDelay",
            EffectType.DeleteNextTurn=>"TurnDelete",
            EffectType.GainEnergy=>"EnergyGain",
            EffectType.AddCardToHand=>"CardAdd",
            EffectType.Gravity=>"Gravity",
            _ => null
        };
    }

    public GameObject GetVFXPrefab()
    {
        // Map effect types to VFX prefabs
        string prefabName = GetEffectName();

        if (prefabName != null)
        {
            GameObject prefab = Resources.Load<GameObject>($"STS/VFX/{prefabName}");
            if (prefab!= null)
            {
                return prefab;
            }
            else
            {
                return Resources.Load<GameObject>($"STS/VFX/{type.ToString()}");
            }
        }
        return null;
    }
    public static EffectEntry Clone(EffectEntry source)
    {
        if (source == null)
            return null;

        var copy = new EffectEntry
        {
            type = source.type,
            value = source.value,
            targetSelf = source.targetSelf,
            targetOthers = source.targetOthers,
            statusType = source.statusType,
            duration = source.duration,
            description = source.description,
            cardID = source.cardID,
            conditional = source.conditional,
            conditionType = source.conditionType,
            conditionValue = source.conditionValue,
            trueEffect = source.trueEffect,
            index = source.index,
            cardSelectionSource = source.cardSelectionSource,
            cardSelectionEffect = source.cardSelectionEffect
        };

        if (source.cardFilterTags != null)
        {
            copy.cardFilterTags = new List<CardFilterTag>();
            foreach (var tag in source.cardFilterTags)
            {
                copy.cardFilterTags.Add(tag);
            }
        }

        return copy;
    }
}