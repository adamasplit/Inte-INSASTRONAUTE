public class SlowStatus : StatusEffect
{
    public SlowStatus(int duration)
    {
        Value = 20; // Default slow value
        Duration = duration;
        Name = "Lenteur";
        modifierType = ModifierType.Additive;
        debuff=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.TurnDelay && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int turnDelay, EffectContext ctx)
    {
        return (turnDelay * (100 + Value)) / 100;
    }
    public override string Desc()
    {
        return $"-{Value}% de vitesse";
    }
}