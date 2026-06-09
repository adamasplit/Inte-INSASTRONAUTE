using UnityEngine;
public class PlatingStatus : StatusEffect
{
    public PlatingStatus(int duration)
    {
        Duration = duration;
        Name = "Blindage";
        buff=true;
        generic=true;
    }
    public override void OnTurnEnd(Character target)
    {
        target.AddArmor(Duration);
    }
    public override string Desc()
    {
        return $"\nGagnez {Duration} d'Armure à la fin de chaque tour";
    }
}