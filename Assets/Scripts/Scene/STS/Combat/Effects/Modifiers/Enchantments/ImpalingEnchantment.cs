using System.Collections.Generic;
public class ImpalingEnchantment : EnchantmentData
{
    public int damagePerLevel = 1;

    public ImpalingEnchantment()
    {
        name = "Impaling";
        description = $"Augmente les dégâts de {damagePerLevel}% par niveau et par Armure sur la cible.";
        maxLevel=5;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new ArmorModifier(StatType.Damage, level * damagePerLevel)
        };
    }
}