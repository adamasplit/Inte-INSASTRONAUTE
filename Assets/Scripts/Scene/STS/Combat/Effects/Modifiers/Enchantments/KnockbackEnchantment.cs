using UnityEngine;
using System.Collections.Generic;
public class KnockbackEnchantment : EnchantmentData
{
    public float knockbackPerLevel = 0.5f;

    public KnockbackEnchantment()
    {
        name = "Knockback";
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