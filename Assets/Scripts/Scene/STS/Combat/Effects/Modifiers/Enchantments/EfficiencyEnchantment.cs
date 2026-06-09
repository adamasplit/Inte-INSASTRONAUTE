using System.Collections.Generic;
public class EfficiencyEnchantment : EnchantmentData
{
    public EfficiencyEnchantment()
    {
        name = "Efficiency";
        description = "Augmente l'efficacité des effets de statut appliqués par la carte.";
        maxLevel=5;
    }
    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.StatusPotency, level*20) // Increase status potency by 20% per level
        };
    }
}