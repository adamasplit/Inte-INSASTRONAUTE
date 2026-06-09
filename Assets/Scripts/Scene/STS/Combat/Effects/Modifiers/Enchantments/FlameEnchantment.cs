using System.Collections.Generic;
public class FlameEnchantment : EnchantmentData
{
    public FlameEnchantment()
    {
        name = "Flame";
        description = $"Inflige des dégâts de feu à la cible en fonction du niveau";
        maxLevel=1;
    }

    public override List<EffectEntry> GenerateEffects(int level)
    {
        return new List<EffectEntry>
        {
            new EffectEntry
            {
                type = EffectType.Status,
                statusType = StatusType.Burn,
                duration=10,
                value=5
            }
        };
    }
}