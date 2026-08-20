using System;
using Newtonsoft.Json.Linq;

public static class AuthoritativeCombatIdentity
{
    public static string GetCombatId(JToken activeCombat)
    {
        string combatId = activeCombat?.Value<string>("combatId");
        if (string.IsNullOrWhiteSpace(combatId))
            throw new ArgumentException("Authoritative combat state must contain a combatId", nameof(activeCombat));

        return combatId;
    }
}
