using UnityEngine;
public class HideStatus : StatusEffect
{
    private bool usedThisTurn=false;
    public HideStatus(int value, int duration)
    {
        Value  = value;
        Duration = duration;
        Name = "Cachette";
        buff=true;
        framed=true;
    }
    public override void OnTurnStart(Character target)
    {
        usedThisTurn=false;
    }
    public override void OnDamageTaken(Character source, Character target, ref int damage)
    {
        if (!usedThisTurn)
        {
            target.AddArmor(Value);
            usedThisTurn=true;
        }
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Pendant {Duration} tours, gagnez {Value} d'Armure la première fois que vous subissez des dégâts";
        }
        return $"L'ennemi gagne {Value} d'Armure la première fois que vous lui infligez des dégâts";
    }
}