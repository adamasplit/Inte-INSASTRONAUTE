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
    public bool conditional;
    public ConditionType conditionType;
    public string conditionValue;
    public bool trueEffect; // For effects that have a "true" version that ignores dispel (like StealBuff vs TrueStealBuff)
    public CardSelectionSource cardSelectionSource; // For effects that involve selecting cards, specify the source of the cards
    public List<CardFilterTag> cardFilterTags = new(); // For effects that involve selecting cards, specify tags to filter the cards
    public CardSelectionEffect cardSelectionEffect; // For effects that involve selecting cards, specify an additional effect to apply to the selected cards
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
            cardSelectionEffect = cardSelectionEffect.ToString()
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
            cardSelectionEffect = Enum.Parse<CardSelectionEffect>(dto.cardSelectionEffect)
        };
    }

    public GameObject GetVFXPrefab()
    {
        // Map effect types to VFX prefabs
        string prefabName = type switch
        {
            EffectType.Damage => "Damage",
            EffectType.Heal => "Heal",
            EffectType.Armor => "Armor",
            EffectType.Status=>StatusEffect.Factory(statusType,0,0,cardID).buff?"Buff":"Debuff",
            EffectType.AdvanceTurn=>"TurnAdvance",
            EffectType.DelayTurn=>"TurnDelay",
            EffectType.DeleteNextTurn=>"TurnDelete",
            EffectType.GainEnergy=>"EnergyGain",
            EffectType.AddCardToHand=>"CardAdd",
            EffectType.Gravity=>"Gravity",
            _ => null
        };

        if (prefabName != null)
        {
            return Resources.Load<GameObject>($"STS/VFX/{prefabName}");
        }
        return null;
    }
}