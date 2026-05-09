public class DivinationStatus : StatusEffect
{
    private int usesThisTurn = 0;
    public DivinationStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Divination";
        buff=true;
    }
    public override string Desc()
    {
        return $"1 fois par tour, annule les dégâts d'une attaque ennemie";
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage&&!ctx.source.isPlayer && ctx.target.statusEffects.Contains(this) && ctx.source != ctx.target && usesThisTurn < Value;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        usesThisTurn++;
        return 0;
    }
}