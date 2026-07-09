using UnityEngine;
public class TimeUntilNextTurnModifier : StatModifier
{
    public int addedValue;

    public TimeUntilNextTurnModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Additive;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value + Mathf.RoundToInt(addedValue * (ctx.combat.turnSystem.GetTimeUntilNextTurn(ctx.target)));
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} par unité de temps jusqu'au prochain tour de la cible";
    }
}