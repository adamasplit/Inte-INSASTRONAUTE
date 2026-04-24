using System.Collections.Generic;
public class EffectContext
{
    public Character source;
    public Character target;
    public CombatManager combat;
    public CombatState state;
    public CardInstance card;
    public List<TurnEntry> timeline;
    public bool isPreview = false; 
}