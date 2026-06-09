using System.Collections.Generic;
public class SharpnessEnchantment : EnchantmentData
{
    public int damagePerLevel = 1;

    public SharpnessEnchantment()
    {
        name = "Sharpness";
        description = $"Inflige des dégâts à la cible en fonction du niveau";
        maxLevel=100;
    }

    public override List<EffectEntry> GenerateEffects(int level)
    {
        return new List<EffectEntry>
        {
            new EffectEntry
            {
                type = EffectType.Damage,
                value = level * damagePerLevel
            }
        };
    }
}