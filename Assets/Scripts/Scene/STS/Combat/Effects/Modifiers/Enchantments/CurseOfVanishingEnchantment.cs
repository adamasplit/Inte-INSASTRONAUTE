using System.Collections.Generic;
public class CurseOfVanishingEnchantment : EnchantmentData
{
    public CurseOfVanishingEnchantment()
    {
        name = "Curse of Vanishing";
        description = "Augmente la chance que la carte s'épuise.";
        maxLevel=10;
    }
    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.ExhaustChance, level * 10)
        };
    }
}