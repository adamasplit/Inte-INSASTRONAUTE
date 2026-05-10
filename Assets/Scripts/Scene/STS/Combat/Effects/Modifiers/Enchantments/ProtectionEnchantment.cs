using System.Collections.Generic;
public class ProtectionEnchantment : EnchantmentData
{
    public int armorPerLevel = 10;

    public ProtectionEnchantment()
    {
        name = "Protection";
        description = $"Augmente l'armure gagnée de {armorPerLevel}% par niveau.";
        maxLevel=100;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.Armor, level * armorPerLevel)
        };
    }
}