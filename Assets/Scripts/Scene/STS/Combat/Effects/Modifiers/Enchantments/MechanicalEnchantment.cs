using System.Collections.Generic;
public class MechanicalEnchantment : EnchantmentData
{
    public MechanicalEnchantment()
    {
        name = "Mécanique";
        description = $"S'active automatiquement à la fin du tour et s'épuise.";
        maxLevel=1;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>();
    }
}