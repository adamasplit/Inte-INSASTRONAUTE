public class HasteStatus : StatusEffect
{
    public HasteStatus(int duration)
    {
        Duration = duration;
        Name = "Célérité";
        modifierType = ModifierType.Additive;
        buff=true;
        generic=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.TurnDelay && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int turnDelay, EffectContext ctx)
    {
        return (turnDelay * (100 - 20)) / 100;
    }
    public override string Desc(bool isPlayer)
    {
        return $"+{20}% de vitesse";
    }
}