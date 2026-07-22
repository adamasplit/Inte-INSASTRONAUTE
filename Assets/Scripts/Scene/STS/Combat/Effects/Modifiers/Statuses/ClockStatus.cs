using System.Collections;
using System.Collections.Generic;
public class ClockStatus : StatusEffect
{
    public ClockStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Horloge";
        buff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Chaque fois que vous infligez des dégâts à un ennemi, son tour est retardé de {Value*2}%.";
        }
        return $"Chaque fois que l'ennemi vous inflige des dégâts, votre tour est retardé de {Value*2}%.";
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        source.GetCombatManager().turnSystem.ApplyDelayAllTurns(target,Value*2);
    }
}