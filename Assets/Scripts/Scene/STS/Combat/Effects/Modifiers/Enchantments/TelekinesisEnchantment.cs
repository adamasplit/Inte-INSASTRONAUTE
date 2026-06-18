using System.Collections.Generic;
public class TelekinesisEnchantment : EnchantmentData
{

    public TelekinesisEnchantment()
    {
        name = "Télékinésie";
        description = $"L'attaque ne peut pas infliger moins de dégâts que sa valeur de base.";
        maxLevel = 1;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>();
    }
}