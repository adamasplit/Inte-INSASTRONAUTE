using UnityEngine;

public class AdrenalineStatus : StatusEffect
{
    public AdrenalineStatus(int value)
    {
        Name = "Adrénaline";
        Value = value;
        Duration = -1;
        buff = true;
        framed = true;
        generic = true;
        modifierType = ModifierType.Multiplicative;
    }

    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.target == owner;
    }

    public override int Modify(int value, EffectContext ctx)
    {
        if (owner == null || owner.maxHP <= 0)
            return value;

        int missingPercent = Mathf.Max(0, (owner.maxHP - owner.currentHP) * 100 / owner.maxHP);
        int reductionPercent = missingPercent / 2;
        return value - value * reductionPercent / 100;
    }

    public override string Desc(bool isPlayer)
    {
        return "Les dégâts reçus diminuent de la moitié du pourcentage de PV que vous avez perdus.";
    }
}