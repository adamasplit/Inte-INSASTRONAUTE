public class DelayImmunityStatus : StatusEffect
{
    public DelayImmunityStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Anti-délai";
        buff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Vos tours ne peuvent pas être retardés";
        }
        return $"Les tours ne peuvent pas être retardés";
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.TurnManipulationDelay &&ctx.target!=null&& ctx.target.statusEffects.Contains(this);
    }
    public override int Modify(int delay, EffectContext ctx)
    {
        return 0;
    }
}