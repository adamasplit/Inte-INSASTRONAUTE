using UnityEngine;
public class RegenStatus : StatusEffect
{
    public RegenStatus(int duration)
    {
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
    public override void OnTurnStart(Character target)
    {
        target.Heal(Duration);
    }
}