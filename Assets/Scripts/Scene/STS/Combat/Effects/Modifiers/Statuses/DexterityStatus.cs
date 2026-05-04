using UnityEngine;
public class DexterityStatus : StatusEffect
{
    public DexterityStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Dextérité";
        modifierType = ModifierType.Additive;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Armor && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int armor, EffectContext ctx)
    {
        return armor + Value;
    }
    public override string Desc()
    {
        return $"+{Value} d'Armure obtenue";
    }
}