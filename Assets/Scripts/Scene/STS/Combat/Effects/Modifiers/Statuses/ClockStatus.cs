using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            return $"Chaque fois que vous infligez des dégâts à une cible, son tour est retardé de {Value*2}%.";
        }
        return $"Chaque fois que le personnage vous inflige des dégâts, votre tour est retardé de {Value*2}%.";
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        if (source == null || target == null)
            return;

        TurnSystem turnSystem = source.GetCombatManager()?.turnSystem;
        if (turnSystem == null)
            return;

        // ApplyDelayAllTurns compte en crans, pas en pourcents : c'est à l'appelant de convertir,
        // comme le fait l'effet DelayTurn (EffectResolver). Le pourcentage annoncé partait ici
        // tel quel, si bien qu'une Horloge (3) promettait 6% et retardait de 6 crans — presque un
        // quart de tour, quatre fois ce qui était écrit.
        float turnDelay = Mathf.Max(1f, target.turnDelay(turnSystem.baseDelay));
        turnSystem.ApplyDelayAllTurns(target, turnDelay * (Value * 2) / 100f);
    }
}