public class HasteStatus : StatusEffect
{
    public HasteStatus(int duration)
    {
        Value = 35; // Default haste value
        Duration = duration;
        Name = "Célérité";
        modifierType = ModifierType.Additive;
        buff=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.TurnDelay && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int turnDelay, EffectContext ctx)
    {
        return (turnDelay * (100 - Value)) / 100;
    }
    public override string Desc()
    {
        return $"+{Value}% de vitesse";
    }
}