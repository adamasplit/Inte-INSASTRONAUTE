using System.Collections.Generic;
public class BlessingEnchantment : EnchantmentData
{
    public int healPerLevel = 10;

    public BlessingEnchantment()
    {
        name = "Blessing";
        description = $"Augmente les soins de {healPerLevel}% par niveau.";
        maxLevel=100;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.Heal, level * healPerLevel)
        };
    }
}