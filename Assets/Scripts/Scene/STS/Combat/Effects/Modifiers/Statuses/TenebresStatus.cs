public class TenebresStatus : StatusEffect
{
    public TenebresStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Ténèbres";
        modifierType = ModifierType.Additive;
        buff = true;
        framed = true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source != null && ctx.target != null
            && ctx.source.statusEffects.Contains(this) && ctx.target.currentHP > ctx.source.currentHP;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return damage + Value;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Quand vous attaquez une cible qui a plus de PV que vous, infligez {Value} dégâts en plus.";
    }
}
