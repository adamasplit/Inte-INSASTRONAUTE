using UnityEngine;
using System.Collections.Generic;
public class FeatherFallingEnchantment : EnchantmentData
{
    public float delayPerLevel = 0.5f;

    public FeatherFallingEnchantment()
    {
        name = "Feather Falling";
        description = $"Avance le tour du lanceur de la carte en fonction du niveau.";
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