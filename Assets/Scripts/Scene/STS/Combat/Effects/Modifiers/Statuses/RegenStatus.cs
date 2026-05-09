using UnityEngine;
public class RegenStatus : StatusEffect
{
    public RegenStatus(int duration)
    {
        Duration = duration;
        Name = "Régénération";
        modifierType = ModifierType.Additive;
        buff=true;
    }
    public override void OnTurnStart(Character target)
    {
        target.Heal(Duration);
    }
    public override string Desc()
    {
        return $"\nSoigne {Duration} PV au début de chaque tour";
    }
}