using UnityEngine;
public class ShieldingStatus : StatusEffect
{
    public ShieldingStatus(int duration)
    {
        Duration = duration;
        Name = "Garde";
        buff=true;
        generic=true;
    }
    public override void OnTurnStart(Character target)
    {
        target.AddArmor(Duration);
    }
    public override string Desc()
    {
        return $"Gagnez {Duration} d'Armure au début de chaque tour";
    }
}