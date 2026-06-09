using System.Collections.Generic;
public class PunchEnchantment : EnchantmentData
{
    public int damagePerLevel = 10;

    public PunchEnchantment()
    {
        name = "Punch";
        description = $"Augmente la puissance des effets de délai de {damagePerLevel}% par niveau.";
        maxLevel=100;
    }

    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.TurnManipulationDelay, level * damagePerLevel)
        };
    }
}