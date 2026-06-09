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
    public Dictionary<Character, int> damageDealtThisCombat = new();
    public bool playerLastTurn;
    public bool killingBlow;
    public bool armorBroken;

    public void ResetActionFlags()
    {
        killingBlow = false;
        armorBroken = false;
    }
    public void ResetTurnFlags(Character character)
    {
        cardsPlayedThisTurn = 0;
        energySpentThisTurn = 0;
        playerLastTurn = character.isPlayer;
    }
}