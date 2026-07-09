using UnityEngine;
public class LostHPModifier : StatModifier
{
    public int addedValue;

    public LostHPModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Multiplicative;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        Debug.Log("LostHPModifier modifying " + value + " with addedValue " + addedValue + " and source currentHP " + (ctx.source != null ? ctx.source.currentHP.ToString() : "null") + " and maxHP " + (ctx.source != null ? ctx.source.maxHP.ToString() : "null"));
        if (ctx.source == null)
            return value;
        return Mathf.RoundToInt(value * (1f + addedValue * (ctx.source.maxHP - ctx.source.currentHP) / 100f));
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} pour chaque % de PV perdus";
    }
} 