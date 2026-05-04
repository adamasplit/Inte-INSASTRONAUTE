using System.Collections.Generic;
public class TelekinesisEnchantment : EnchantmentData
{

    public TelekinesisEnchantment()
    {
        name = "Télékinésie";
        description = $"Empêche l'attaque d'avoir des dégâts réduits.";
        maxLevel = 1;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>();
    }
}