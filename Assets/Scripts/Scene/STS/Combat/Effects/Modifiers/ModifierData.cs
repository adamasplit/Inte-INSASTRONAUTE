using System;
using UnityEngine;
[System.Serializable]
public class ModifierData
{
    public StatType type;
    public ModifierKind kind;
    public int value;
    public string info;
    public string description;
    public StatModifier CreateModifier()
    {
        StatModifier modifier = CreateStatModifier();
        modifier.description = description;
        return modifier;
    }
    private StatModifier CreateStatModifier()
    {
        switch (kind)
        {
            case ModifierKind.Flat:
                return new FlatModifier(type, value);
            case ModifierKind.Discard:
                return new DiscardModifier(type, value);
            case ModifierKind.Played:
                return new PlayedModifier(type, value);
            case ModifierKind.DebuffOnSelf:
                return new DebuffOnSelfModifier(type, value);
            case ModifierKind.BuffOnSelf:
                return new BuffOnSelfModifier(type, value);
            case ModifierKind.ArmorScaling:
                return new ArmorModifier(type, value);
            case ModifierKind.TargetNumber:
                return new TargetNumberModifier(type, value);
            case ModifierKind.DebuffOnTarget:
                return new DebuffOnTargetModifier(type, value);
            case ModifierKind.BuffOnTarget:
                return new BuffOnTargetModifier(type, value);
            case ModifierKind.ArmorOnSelf:
                return new ArmorOnSelfModifier(type, value);
            case ModifierKind.LostHP:
                return new LostHPModifier(type, value);
            case ModifierKind.TargetLostHP:
                return new TargetLostHPModifier(type, value);
            case ModifierKind.TimeUntilNextTurn:
                return new TimeUntilNextTurnModifier(type, value);
            case ModifierKind.ArmorOnTarget:
                return new ArmorOnTargetModifier(type, value);
            case ModifierKind.HPLostSinceLastTurn:
                return new HPLostSinceLastTurnModifier(type, value);
            case ModifierKind.DamageDealtWithLastAction:
                return new DamageDealtWithLastActionModifier(type, value);
            default:
                throw new System.Exception("Unknown modifier kind: " + kind);
        }
    }
    public ModifierDTO ToDTO()
    {
        return new ModifierDTO
        {
            type = type.ToString(),
            kind = kind.ToString(),
            value = value,
            info = info,
            description = description
        };
    }
    public static ModifierData FromDTO(ModifierDTO dto)
    {
        return new ModifierData
        {
            type = Enum.Parse<StatType>(dto.type),
            kind = Enum.Parse<ModifierKind>(dto.kind),
            value = dto.value,
            info = dto.info,
            description = dto.description
        };
    }
}