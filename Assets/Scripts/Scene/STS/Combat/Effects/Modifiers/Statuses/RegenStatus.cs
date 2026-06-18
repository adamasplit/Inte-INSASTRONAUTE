using UnityEngine;
public class RegenStatus : StatusEffect
{
    public RegenStatus(int duration)
    {
        Duration = duration;
        Name = "Régénération";
        modifierType = ModifierType.Additive;
        buff=true;
        generic=true;
    }
    public override void OnTurnStart(Character target)
    {
        target.Heal(Duration);
        Tick(target);
    }
    public override void OnTurnEnd(Character target)
    {
    }
    public override string Desc(bool isPlayer)
    {
        return $"Soigne {Duration} PV au début de chaque tour";
    }
}