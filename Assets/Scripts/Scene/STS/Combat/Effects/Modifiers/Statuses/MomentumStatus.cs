public class MomentumStatus : StatusEffect
{
    public MomentumStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Élan";
        buff = true;
        framed = true;
        modifierType = ModifierType.Additive;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source != null
            && ctx.source.statusEffects.Contains(this) && ctx.card != null
            && ctx.card.Type == CardType.Attaque;
    }

    public override int Modify(int damage, EffectContext ctx)
    {
        return AppliesTo(StatType.Damage, ctx) ? damage + Value : damage;
    }

    public override string Desc(bool isPlayer)
    {
        return $"Les cartes non-Attaque gagnent 1 Élan. Votre prochaine Attaque inflige {Value} dégâts supplémentaires et consomme cet Élan.";
    }
}
