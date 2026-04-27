using UnityEngine;
public class PoisonStatus : StatusEffect
{
    public PoisonStatus(int duration)
    {
        Duration = duration;
        Name = "Poison";
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
        target.TakeDamage(Duration);
    }
}