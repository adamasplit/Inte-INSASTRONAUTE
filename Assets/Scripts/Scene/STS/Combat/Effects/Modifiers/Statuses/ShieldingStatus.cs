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
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Vous gagnez {Duration} d'Armure au début de votre tour";
        }
        return $"Gagne {Duration} d'Armure au début de son tour";
    }
}