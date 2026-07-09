using System.Linq;
public class DebuffOnTargetModifier : StatModifier
{
    public int perDebuff = 2;
    public DebuffOnTargetModifier(StatType type, int amount)
    {
        this.type = type;
        perDebuff = amount;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        return value + ctx.target.statusEffects.Where(s => !s.buff).Count() * perDebuff;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perDebuff,modifierType)} par effet négatif sur la cible";
    }
}