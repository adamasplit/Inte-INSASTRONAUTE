using System.Collections;
using System.Collections.Generic;
public class ClockStatus : StatusEffect
{
    public ClockStatus()
    {
        Value = 1;
        Duration = -1;
        Name = "Horloge";
        buff=true;
        framed=true;
    }
    public override string Desc()
    {
        return $"Chaque fois que vous infligez des dégâts à un ennemi, son tour est retardé de {Value}.";
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        source.GetCombatManager().turnSystem.ApplyDelayAllTurns(target,Value);
    }
}