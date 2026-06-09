using System.Linq;
public class BuffOnSelfModifier : StatModifier
{
    public int perBuff = 2;
    public BuffOnSelfModifier(StatType type, int amount)
    {
        this.type = type;
        perBuff = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.source == null)
            return value;
        return value + ctx.source.statusEffects.Where(s => s.buff).Count() * perBuff;
    }

    public override string Describe()
    {
        return $"+{perBuff} {StatTypeString.ToFrench(type)} par effet bénéfique actif";
    }
}