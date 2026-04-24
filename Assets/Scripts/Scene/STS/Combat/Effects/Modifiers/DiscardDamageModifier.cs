public class DiscardDamageModifier : StatModifier
{
    public int perDiscard = 2;
    public DiscardDamageModifier(Character owner, int amount)
    {
        Owner = owner;
        type = StatType.Damage;
        perDiscard = amount;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source == Owner;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        return value + ctx.state.cardsDiscardedThisCombat * perDiscard;
    }

    public override string Describe()
    {
        return $"+{perDiscard} damage per discarded card";
    }
}