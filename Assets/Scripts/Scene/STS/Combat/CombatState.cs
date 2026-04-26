using System.Collections.Generic;
public class CombatState
{
    public List<StatModifier> modifiers = new();

    public List<StatModifier> GetModifiers(StatType type)
    {
        return modifiers.FindAll(m => m.type == type);
    }

    public int cardsDiscardedThisCombat;
    public int cardsPlayedThisTurn;
    public int energySpentThisTurn;
}