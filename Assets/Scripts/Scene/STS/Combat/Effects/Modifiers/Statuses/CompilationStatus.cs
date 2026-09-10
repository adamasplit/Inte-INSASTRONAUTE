using UnityEngine;

public class CompilationStatus : StatusEffect
{
    public CompilationStatus(int value)
    {
        Name = "Compilation";
        Value = value;
        Duration = -1;
        debuff = true;
        framed = true;
    }

    public override void OnHPLoss(Character target, int damage)
    {
        if (damage <= 0 || target == null || !target.onTurn || target.combat == null
            || target.combat.turnSystem == null)
            return;

        TurnEntry entry = target.combat.turnSystem.currentTurnEntry;
        if (entry == null || entry.character != target)
            return;

        float turnDelay = Mathf.Max(1f, target.turnDelay(target.combat.turnSystem.baseDelay));
        target.combat.turnSystem.AddPendingDelay(entry, turnDelay * Value / 100f);
    }

    public override string Desc(bool isPlayer)
    {
        return $"Quand la cible perd des PV pendant son tour, son tour suivant est retardé de {Value}%.";
    }
}