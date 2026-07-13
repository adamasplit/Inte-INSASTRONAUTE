using UnityEngine;
public class ArmorOnTargetModifier : StatModifier
{
    public int addedValue;

    public ArmorOnTargetModifier(StatType type, int amount)
    {
        this.type = type;
        addedValue = amount;
        modifierType = ModifierType.Additive;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        int targetArmor = 0;
        foreach(var target in ctx.targets)
        {
            targetArmor+= target.armor;
        }
        return value + addedValue * targetArmor;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} par Armure de la cible";
    }
}