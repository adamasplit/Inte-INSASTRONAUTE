using System;

public static class AuthoritativeCombatIdentity
{
    public static string GetTransportId(string runId, object activeCombat)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Authoritative combat transport requires a run id", nameof(runId));
        if (activeCombat == null)
            throw new ArgumentNullException(nameof(activeCombat));

        return runId;
    }

    /// <summary>
    /// Un duel s'adresse par son identifiant de bataille : c'est lui qui compose le sujet
    /// de la souscription, la destination des commandes et l'URL du snapshot, et c'est lui
    /// que chaque message doit porter comme <c>combatId</c> pour que le noyau du pont
    /// l'accepte.
    /// </summary>
    public static string GetPvpTransportId(string battleId)
    {
        if (string.IsNullOrWhiteSpace(battleId))
            throw new ArgumentException(
                "A PvP battle transport requires a battle id", nameof(battleId));

        return battleId;
    }
}
