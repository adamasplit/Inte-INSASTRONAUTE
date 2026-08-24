using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// Reads the combatant list out of an authoritative combat state.
///
/// <para>The dead are kept: the server holds them in its state with zero hit points,
/// and it is their presence that stops their neighbours' identities from shifting. A
/// malformed combatant is skipped rather than completed with plausible values.</para>
/// </summary>
public static class CombatantSnapshotReader
{
    public static IReadOnlyList<CombatantDescriptor> ReadCombatants(
        JToken combatToken,
        string localCombatantId)
    {
        var result = new List<CombatantDescriptor>();
        if (!(combatToken is JObject combat) || !(combat["combatants"] is JArray combatants))
            return result;

        foreach (JToken combatantToken in combatants)
        {
            if (!(combatantToken is JObject combatant))
                continue;

            string combatantId = combatant.Value<string>("combatantId");
            string teamId = combatant.Value<string>("teamId");
            if (string.IsNullOrWhiteSpace(combatantId) || string.IsNullOrWhiteSpace(teamId))
                continue;

            result.Add(new CombatantDescriptor(
                combatantId,
                teamId,
                ReadController(combatant.Value<string>("controllerType")),
                string.Equals(combatantId, localCombatantId, StringComparison.Ordinal)));
        }
        return result;
    }

    /// <summary>
    /// A missing or unknown controller type counts as AI: assuming a human would hand
    /// the turn to a combatant the player does not drive.
    /// </summary>
    private static CombatantController ReadController(string controllerType)
    {
        return string.Equals(controllerType, "HUMAN", StringComparison.OrdinalIgnoreCase)
            ? CombatantController.Human
            : CombatantController.Ai;
    }
}
