using System.Collections.Generic;
public class CombatState
{
    public int turnCount=0;
    public List<StatModifier> modifiers = new();

    public List<StatModifier> GetModifiers(StatType type)
    {
        return modifiers.FindAll(m => m.type == type);
    }

    public int cardsDiscardedThisCombat;
    
    public int energySpentThisTurn;
    public int energyGainedThisTurn;
    public Dictionary<Character, int> damageDealtThisCombat = new();
    public Dictionary<Character, int> hpLostSinceLastTurn = new();
    public bool playerLastTurn;
    public bool killingBlow;
    public bool armorBroken;
    public int damageDealtWithLastAction;
    public List<CardInstance> cardsPlayedThisCombat = new();
    public List<CardInstance> cardsPlayedThisTurn = new();
    public void ResetActionFlags()
    {
        killingBlow = false;
        armorBroken = false;
        damageDealtWithLastAction = 0;
    }
    public void ResetTurnStartFlags(Character character)
    {
        turnCount++;
        cardsPlayedThisTurn.Clear();
        energySpentThisTurn = 0;
        energyGainedThisTurn = 0;
        playerLastTurn = character.isPlayer;
    }
    public void ResetTurnEndFlags(Character character)
    {
        hpLostSinceLastTurn[character] = 0;
    }
}