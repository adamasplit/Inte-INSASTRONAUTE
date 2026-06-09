public class FragileStatus : StatusEffect
{
    public FragileStatus(int duration)
    {
        Duration = duration;
        Name = "Fragilité";
        modifierType = ModifierType.Additive;
        buff=false;
        debuff=true;
        generic=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Armor && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int armorGain, EffectContext ctx)
    {
        return (armorGain * (100 - 25)) / 100;
    }
    public override string Desc()
    {
        return $"-25% d'armure gagnée";
    }
}