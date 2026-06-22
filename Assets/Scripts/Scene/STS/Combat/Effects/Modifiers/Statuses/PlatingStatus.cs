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
        Tick(target);
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Vous gagnez {Duration} d'Armure à la fin de votre tour";
        }
        return $"L'ennemi gagne {Duration} d'Armure à la fin de son tour";
    }
}