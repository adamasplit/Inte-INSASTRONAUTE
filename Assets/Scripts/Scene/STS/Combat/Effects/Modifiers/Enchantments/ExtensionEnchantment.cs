using System.Collections.Generic;
public class ExtensionEnchantment : EnchantmentData
{
    public ExtensionEnchantment()
    {
        name = "Extension";
        description = "Augmente la durée des effets de statut appliqués par la carte.";
        maxLevel=5;
    }
    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.StatusDuration, level*20) // Increase status duration by 20% per level
        };
    }
}