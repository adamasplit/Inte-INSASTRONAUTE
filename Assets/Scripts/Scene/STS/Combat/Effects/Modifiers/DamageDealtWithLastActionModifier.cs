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
        Debug.Log("DamageDealtWithLastActionModifier modifying " + value + " with addedValue " + addedValue + " and damageDealtWithLastAction " + ctx.combat.state.damageDealtWithLastAction);
        if (ctx.target == null)
            return value;
        Debug.Log("DamageDealtWithLastActionModifier modifying " + value + " with addedValue " + addedValue + " and damageDealtWithLastAction " + ctx.combat.state.damageDealtWithLastAction);
        return value + ctx.combat.state.damageDealtWithLastAction;
    }

    public override string Describe()
    {
        return $"+ {addedValue} {StatTypeString.ToFrench(type)} pour chaque dégât infligé avec cette action";
    }
}