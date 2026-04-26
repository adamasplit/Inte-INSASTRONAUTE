using UnityEngine;
public class RegenStatus : StatusEffect
{
    public RegenStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Régénération";
        modifierType = ModifierType.Additive;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return false;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return damage;
    }
    public override void OnTurnEnd(Character target)
    {
        target.Heal(Value);
    }
}