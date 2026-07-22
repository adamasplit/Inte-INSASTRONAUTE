using UnityEngine;
using System.Collections.Generic;
public class KnockbackEnchantment : EnchantmentData
{
    public float knockbackPerLevel = 2f;

    public KnockbackEnchantment()
    {
        name = "Recul";
        description = $"Repousse le tour de la cible en fonction du niveau.";
        maxLevel=100;
    }

    public override List<EffectEntry> GenerateEffects(int level)
    {
        return new List<EffectEntry>
        {
            new EffectEntry
            {
                type = EffectType.DelayTurn,
                value = Mathf.CeilToInt(level * knockbackPerLevel)
            }
        };
    }
}