using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class DamageDealtWithLastActionModifier : StatModifier
{
    private int addedValue = 1;
    public DamageDealtWithLastActionModifier(StatType type,int amount)
    {
        this.type = type;
        this.addedValue = amount;
        modifierType = ModifierType.Additive;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value + ctx.combat.state.damageDealtWithLastAction;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} pour chaque dégât infligé avec cette action";
    }
}