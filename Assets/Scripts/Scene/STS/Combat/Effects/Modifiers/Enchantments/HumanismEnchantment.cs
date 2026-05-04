using System.Collections.Generic;
public class HumanismEnchantment : EnchantmentData
{

    public HumanismEnchantment()
    {
        name = "Humanisme";
        description = $"Traverse l'Armure";
        maxLevel = 1;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>();
    }
}