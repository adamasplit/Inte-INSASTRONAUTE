using System.Collections.Generic;
public class ReplayEnchantment : EnchantmentData
{
    public ReplayEnchantment()
    {
        name = "Écho";
        description = "Permet de rejouer la carte un certain nombre de fois. Ses effets sont réduits en conséquence.";
        maxLevel=10;
    }
    public int GetReplayCount(int level)
    {
        return level; // 1 replay per level
    }
    public override List<StatModifier> GenerateModifiers(int level)
    {
        return new List<StatModifier>
        {
            new PercentModifier(StatType.Any, -10 / (GetReplayCount(level)))
        };
    }
}