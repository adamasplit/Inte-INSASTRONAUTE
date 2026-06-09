using System.Collections.Generic;
public class UnbreakingEnchantment : EnchantmentData
{
    public UnbreakingEnchantment()
    {
        name = "Unbreaking";
        description = "Donne une chance de ne pas épuiser la carte.";
        maxLevel=10;
    }
    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.ExhaustChance, -level * 10)
        };
    }
}