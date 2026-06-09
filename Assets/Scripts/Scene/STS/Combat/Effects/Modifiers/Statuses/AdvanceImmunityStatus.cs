public class AdvanceImmunityStatus : StatusEffect
{
    public AdvanceImmunityStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Anti-avancement";
        debuff=true;
        framed=true;
    }
    public override string Desc()
    {
        return $"Les tours ne peuvent pas être avancés";
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.TurnManipulationAdvance && ctx.target.statusEffects.Contains(this);
    }
    public override int Modify(int delay, EffectContext ctx)
    {
        return 0;
    }
}