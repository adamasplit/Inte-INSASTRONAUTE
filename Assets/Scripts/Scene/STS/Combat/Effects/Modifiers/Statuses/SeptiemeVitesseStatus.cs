public class SeptiemeVitesseStatus : StatusEffect
{
    public SeptiemeVitesseStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Septième vitesse";
        modifierType = ModifierType.Multiplicative;
        buff = true;
        framed = true;
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source != null && ctx.source.statusEffects.Contains(this)
            && ctx.card != null && ctx.card.Type == CardType.Attaque;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return damage + (damage * Value * 20 / 100);
    }
    public override void OnCardPlayed(Character source, Character target, CardInstance card)
    {
        if (card != null && card.Type == CardType.Attaque)
        {
            source.LoseHP(Value);
        }
    }
    public override string Desc(bool isPlayer)
    {
        return $"Vous perdez {Value} PV à chaque Attaque jouée et infligez {Value * 20}% de dégâts supplémentaires.";
    }
}
