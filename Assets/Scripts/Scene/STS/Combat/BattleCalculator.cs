using UnityEngine;
using System.Collections.Generic;
public static class BattleCalculator
{
    public static int GetModifiedValue(int baseValue, StatType type, EffectContext ctx)
    {
        int value = baseValue;

        foreach (var mod in GetAllModifiers(type, ctx.card, ctx.state))
            value = mod.Modify(value, ctx);
        if (ctx.source != null)
        {
            foreach (var effect in ctx.source.statusEffects)
            {
                if (effect.AppliesTo(type, ctx))
                    value = effect.Modify(value, ctx);
            }
        }
        if (ctx.target != null)
        {
            foreach (var effect in ctx.target.statusEffects)
            {
                if (effect.AppliesTo(type, ctx))
                    value = effect.Modify(value, ctx);
            }
        }

        return value;
    }
    public static string GetModifiedDescription(int baseValue, StatType type, EffectContext ctx)
    {
        int modifiedValue = GetModifiedValue(baseValue, type, ctx);
        if (modifiedValue <= 0)
            return $"<color=gray>{modifiedValue}</color>";
        else if (modifiedValue < baseValue)
            return $"<color=red>{modifiedValue}</color>";
        else if (modifiedValue > baseValue)
            return $"<color=green>{modifiedValue}</color>";
        else
            return modifiedValue.ToString();
    }

    private static List<StatModifier> GetAllModifiers(StatType type,CardInstance card, CombatState state)
    {
        List<StatModifier> modifiers = new();
        if (state != null)
        modifiers.AddRange(state.GetModifiers(type));
        if (card != null)
        modifiers.AddRange(card.GetModifiers(type));
        return modifiers;
    }
}