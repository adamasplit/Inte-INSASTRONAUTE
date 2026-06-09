using System;
using UnityEngine;
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
    public EffectEntryDTO ToDTO()
    {
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
            trueEffect = trueEffect
        };
    }
    public static EffectEntry FromDTO(EffectEntryDTO dto)
    {
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
            trueEffect = dto.trueEffect
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
            EffectType.Status=>StatusEffect.Factory(statusType,0,0).buff?"Buff":"Debuff",
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