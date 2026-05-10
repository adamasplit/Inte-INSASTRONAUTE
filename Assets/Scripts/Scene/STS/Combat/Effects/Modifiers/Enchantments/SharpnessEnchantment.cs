using System.Collections.Generic;
public class SharpnessEnchantment : EnchantmentData
{
    public int damagePerLevel = 10;

    public SharpnessEnchantment()
    {
        name = "Sharpness";
        description = $"Augmente les dégâts de {damagePerLevel}% par niveau.";
        maxLevel=100;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.Damage, level * damagePerLevel)
        };
    }
}