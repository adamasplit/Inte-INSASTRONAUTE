using System.Linq;
public class DebuffOnSelfModifier : StatModifier
{
    public int perDebuff = 2;
    public DebuffOnSelfModifier(StatType type, int amount)
    {
        this.type = type;
        perDebuff = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.source == null)
            return value;
        return value + ctx.source.statusEffects.Where(s => s.debuff).Count() * perDebuff;
    }

    public override string Describe()
    {
        return $"+{perDebuff} {StatTypeString.ToFrench(type)} par effet néfaste actif";
    }
}