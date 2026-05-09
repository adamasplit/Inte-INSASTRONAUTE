using System;
using UnityEngine;
[Serializable]
public class EffectEntry
{
    public EffectType type;
    public int value;
    public bool targetSelf=false;
    public StatusType statusType;
    public int duration;
    public string description; // Optional custom description for the effect
    public EffectEntryDTO ToDTO()
    {
        return new EffectEntryDTO
        {
            type = type.ToString(),

            value = value,

            targetSelf = targetSelf,

            statusType = statusType.ToString(),

            duration = duration,

            description = description
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

            duration = dto.duration,

            description = dto.description
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
            _ => null
        };

        if (prefabName != null)
        {
            return Resources.Load<GameObject>($"STS/VFX/{prefabName}");
        }
        return null;
    }
}