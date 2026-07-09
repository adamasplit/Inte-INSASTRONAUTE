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
        Debug.Log($"ArmorOnTargetModifier: ctx.target={ctx.target}, ctx.targets.Count={ctx.targets.Count}");
        if (ctx.target == null)
            return value;
        int targetArmor = 0;
        foreach(var target in ctx.targets)
        {
            Debug.Log($"ArmorOnTargetModifier: target={target}, target.armor={target.armor}");
            targetArmor+= target.armor;
        }
        return value + addedValue * targetArmor;
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, addedValue,modifierType)} par Armure de la cible";
    }
}