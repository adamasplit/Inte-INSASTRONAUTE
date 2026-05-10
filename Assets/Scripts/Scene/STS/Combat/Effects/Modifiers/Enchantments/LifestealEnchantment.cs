public class LifestealEnchantment : EnchantmentData
{
    public int lifestealPercentPerLevel = 5;

    public LifestealEnchantment()
    {
        name = "Lifesteal";
        description = $"Rend {lifestealPercentPerLevel}% des dégâts infligés par niveau.";
        maxLevel=100;
    }
    public int healPercent(int level)
    {
        return level * lifestealPercentPerLevel;
    }
}