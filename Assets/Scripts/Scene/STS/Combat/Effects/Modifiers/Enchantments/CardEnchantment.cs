using System.Collections.Generic;
public class CardEnchantment
{
    public EnchantmentData data;
    public int level;

    public List<StatModifier> GetModifiers()
    {
        return data.GenerateModifiers(level);
    }
}