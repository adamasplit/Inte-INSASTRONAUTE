using UnityEngine;
using System.Collections.Generic;
public class FeatherFallingEnchantment : EnchantmentData
{
    public float delayPerLevel = 2f;

    public FeatherFallingEnchantment()
    {
        name = "Légèreté";
        description = $"Avance le tour du joueur en fonction du niveau.";
        maxLevel=100;
    }

    public override List<EffectEntry> GenerateEffects(int level)
    {
        return new List<EffectEntry>
        {
            new EffectEntry
            {
                type = EffectType.AdvanceTurn,
                value = Mathf.CeilToInt(level * delayPerLevel),
                targetSelf = true
            }
        };
    }
}