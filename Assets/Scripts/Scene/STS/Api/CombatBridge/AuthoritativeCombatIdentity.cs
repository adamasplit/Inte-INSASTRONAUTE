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
}
