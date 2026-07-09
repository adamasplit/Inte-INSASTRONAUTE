using System.Collections.Generic;
public class FlameEnchantment : EnchantmentData
{
    public FlameEnchantment()
    {
        name = "Aura de feu";
        description = $"Inflige Brûlure à la cible en fonction du niveau";
        maxLevel=5;
    }

    public override List<EffectEntry> GenerateEffects(int level)
    {
        return new List<EffectEntry>
        {
            new EffectEntry
            {
                type = EffectType.Status,
                statusType = StatusType.Burn,
                duration=level*2,
                value=5
            }
        };
    }
}