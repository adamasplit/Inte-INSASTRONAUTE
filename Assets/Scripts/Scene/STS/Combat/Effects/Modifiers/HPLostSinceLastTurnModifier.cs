using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class HPLostSinceLastTurnModifier : StatModifier
{
    private int addedValue = 1;
    public HPLostSinceLastTurnModifier(StatType type,int amount)
    {
        this.type = type;
        this.addedValue = amount;
        modifierType = ModifierType.Additive;
    }

    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value + ctx.combat.state.hpLostSinceLastTurn.GetValueOrDefault(ctx.source, 0);
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} pour chaque PV perdu depuis le dernier tour";
    }
}