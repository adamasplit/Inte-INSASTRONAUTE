using System.Collections.Generic;
public class SweepingEdgeEnchantment : EnchantmentData
{
    public int damagePerLevel = 10;

    public SweepingEdgeEnchantment()
    {
        name = "Sweeping Edge";
        description = $"Augmente les dégâts de {damagePerLevel}% par niveau et par cible touchée.";
        maxLevel=3;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new TargetNumberModifier(StatType.Damage, level * damagePerLevel)
        };
    }
}