using System.Collections.Generic;
public class AstraEnchantment : EnchantmentData
{
    // Astra is a reference to the Fire Emblem skill that allows a unit to strike multiple times with reduced damage. In this case, the card will be played 5 times but all its effects will be reduced to 1.
    public AstraEnchantment()
    {
        name = "Stellaire";
        description = $"La carte se joue 5 fois mais la plupart de ses effets sont réduits à 1.";
        maxLevel=1;
    }
    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new OverrideModifier(StatType.Any, 1),
            new FlatModifier(StatType.ReplayCount, 4)
        };
    }
}