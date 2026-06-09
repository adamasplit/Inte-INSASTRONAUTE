using UnityEngine;
using System.Collections.Generic;
public static class BattleCalculator
{
    public static int GetModifiedValue(int baseValue, StatType type, EffectContext ctx)
    {
        int value = baseValue;

        List<StatModifier> modifiers = GetAllModifiers(type, ctx.card, ctx.state);
        List<StatModifier> applyingModifiers=new List<StatModifier>();
        foreach (var mod in modifiers)
        {
            if (mod.AppliesTo(type, ctx))
                applyingModifiers.Add(mod);
        }
        if (ctx.source != null)
        {
            foreach (var effect in ctx.source.statusEffects)
            {
                if (effect.AppliesTo(type, ctx))
                    applyingModifiers.Add(effect);
            }
        }
        if (ctx.target != null&&ctx.target!=ctx.source)
        {
            foreach (var effect in ctx.target.statusEffects)
            {
                if (effect.AppliesTo(type, ctx))
                    applyingModifiers.Add(effect);
            }
        }
        // Apply modifiers in the correct order: Additive first, then Multiplicative, then Override

        applyingModifiers.Sort((a, b) => a.modifierType.CompareTo(b.modifierType));
        foreach (var mod in applyingModifiers)
        {
            if (mod.AppliesTo(type, ctx))
                value = mod.Modify(value, ctx);
        }
        
        


        // Effets spéciaux non résumables à des modificateurs, comme la télékinésie
        if (ctx.card!=null)
        {
            if (ctx.card.enchantments.Exists(e=>e.data.name=="Télékinésie"))
            {
                value = Mathf.Max(value, baseValue);
            }
        }
        return value;
    }
    public static string GetModifiedDescription(int baseValue, StatType type, EffectContext ctx)
    {
        int modifiedValue = GetModifiedValue(baseValue, type, ctx);
        return GetColoredValue(baseValue, modifiedValue);
    }
    public static string GetColoredValue(int baseValue, int modifiedValue)
    {
        if (modifiedValue <= 0)
            return $"<color=grey>{modifiedValue}</color>";
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
        modifiers.AddRange(card.GetModifiers());

        // Ensure ordering: Additive first, then Multiplicative, then Override
        modifiers.Sort((a, b) => a.modifierType.CompareTo(b.modifierType));

        return modifiers;
    }
    
}