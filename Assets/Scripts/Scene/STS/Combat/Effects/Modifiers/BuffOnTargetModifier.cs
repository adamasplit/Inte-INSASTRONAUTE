using System.Linq;
public class BuffOnTargetModifier : StatModifier
{
    public int perBuff = 2;
    public BuffOnTargetModifier(StatType type, int amount)
    {
        this.type = type;
        perBuff = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value + ctx.target.statusEffects.Where(s => s.buff).Count() * perBuff;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perBuff,modifierType)} par effet bénéfique sur la cible";
    }
}