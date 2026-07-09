using System.Collections.Generic;
using UnityEngine;
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
            new PercentModifier(StatType.Any, Mathf.RoundToInt(100f*level switch
            {
                1 => -0.5f,
                2 => -0.67f,
                3 => -0.75f,
                4 => -0.80f,
                5 => -0.83f,
                6 => -0.85f,
                7 => -0.86f,
                8 => -0.875f,
                9 => -0.888f,
                10 => -0.90f,
                _ => -1f
            })),
            new FlatModifier(StatType.ReplayCount, GetReplayCount(level))
        };
    }
}