using System.Linq;
using System.Collections.Generic;
public class BrandStatus:StatusEffect
{
    public BrandStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Marque";
        debuff=true;
        generic=true;
        framed=true;
        modifierType = ModifierType.Additive;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.target.statusEffects.Contains(this);
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        int debuffCount = ctx.target.statusEffects.Where(se=>se.debuff && se != this).Count();
        return damage + (damage * Value * debuffCount) / 100;
    }
    public override string Desc(bool isPlayer)
    {
        return $"+{Value}% dégâts subis par debuff sur la cible.";
    }
}