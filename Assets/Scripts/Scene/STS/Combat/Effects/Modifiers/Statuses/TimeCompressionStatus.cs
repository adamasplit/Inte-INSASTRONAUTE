using System.Linq;
using UnityEngine;

/// <summary>
/// Compression temporelle : chaque coup non bloqué efface le prochain tour de sa cible.
///
/// <para>« Non bloqué » se lit sur les points de vie perdus : c'est ce qui a traversé, quelle que
/// soit l'armure qu'il a fallu manger d'abord.</para>
/// </summary>
public class TimeCompressionStatus : StatusEffect
{
    public TimeCompressionStatus(int duration)
    {
        Duration = duration;
        Name = "Compression temporelle";
        buff = true;
        framed = true;
        goldFrame = true;
    }

    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        if (owner == null || source != owner || target == null || damage <= 0)
            return;
        // L'armure absorbe d'abord : un coup qu'elle encaisse entièrement n'a rien traversé.
        if (target.armor >= damage)
            return;

        DeleteNextTurn(source, target);
    }

    /// <summary>
    /// Repousse d'un tour complet la prochaine entrée de la cible, comme le fait l'effet
    /// DeleteNextTurn : le tour n'est pas retiré de la file mais reculé, ce qui revient au même
    /// pour qui le perd et laisse à chacun un tour à venir.
    /// </summary>
    private static void DeleteNextTurn(Character source, Character target)
    {
        var turnSystem = source.combat != null ? source.combat.turnSystem : null;
        if (turnSystem == null || turnSystem.timeline == null || turnSystem.timeline.Count == 0)
            return;

        var entry = turnSystem.timeline
            .Where(t => t != null && t.character == target && t != turnSystem.timeline.First())
            .OrderBy(t => t.time)
            .FirstOrDefault();
        if (entry == null)
            return;

        entry.time += Mathf.Max(1f, target.turnDelay(turnSystem.baseDelay));
    }

    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Pendant {turns}, chacun de vos dégâts non bloqués supprime le prochain tour de la cible.";
        }
        return $"Pendant {turns}, chaque dégât non bloqué de ce personnage supprime le prochain tour de la cible.";
    }
}
