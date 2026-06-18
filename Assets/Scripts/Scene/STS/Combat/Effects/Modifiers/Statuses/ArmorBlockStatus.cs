public class ArmorBlockStatus : StatusEffect
{
    public ArmorBlockStatus(int duration)
    {
        Duration = duration;
        Name = "Anti-armure";
        modifierType = ModifierType.Override;
        buff=false;
        debuff=true;
        framed=true;
        goldFrame=true;
        inextendable=true;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Armor && ctx.source.statusEffects.Contains(this);
    }

    public override int Modify(int armorGain, EffectContext ctx)
    {
        return 0;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Vous ne pouvez plus gagner d'armure pendant {Duration} tours.";
        }
        return $"La cible ne peut plus gagner d'armure pendant {Duration} tours.";
    }
}