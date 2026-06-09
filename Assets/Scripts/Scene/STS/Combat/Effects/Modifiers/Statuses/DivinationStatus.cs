public class DivinationStatus : StatusEffect
{
    public DivinationStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Divination";
        buff=true;
        framed=true;
    }
    public override string Desc()
    {
        return $"Annule les dégâts d'une attaque ennemie. Se déclenche {Value} fois.";
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage&&!ctx.source.isPlayer && ctx.target.statusEffects.Contains(this) && ctx.source != ctx.target;
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        if (!ctx.isPreview) Value--;
        return 0;
    }
}