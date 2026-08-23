using System;
using System.Collections.Generic;

/// <summary>
/// The mapping between the combatants the server names and the objects the client
/// draws.
///
/// <para>It exists for one reason: a combatant's identity must never be inferred from
/// its position in a list. The server assigns ids once and keeps the dead in its
/// state, while the client drops the dead from its display lists. Deriving one from
/// the other makes a combatant's state land on its neighbour the moment anyone
/// dies.</para>
///
/// <para>The combatant type is a generic parameter because this assembly does not
/// reference the Unity engine — that is what makes this class testable.</para>
/// </summary>
public sealed class CombatantRegistry<TCombatant> where TCombatant : class
{
    private readonly Dictionary<string, CombatantDescriptor> descriptors =
        new Dictionary<string, CombatantDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, TCombatant> combatantsById =
        new Dictionary<string, TCombatant>(StringComparer.Ordinal);
    private readonly List<KeyValuePair<string, TCombatant>> registrationOrder =
        new List<KeyValuePair<string, TCombatant>>();

    private string localCombatantId;
    private string localTeamId;

    public string LocalCombatantId => localCombatantId;

    public void Register(CombatantDescriptor descriptor, TCombatant combatant)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));
        if (combatant == null)
            throw new ArgumentNullException(nameof(combatant));
        if (descriptors.ContainsKey(descriptor.CombatantId))
            throw new InvalidOperationException(
                "Combatant already registered: " + descriptor.CombatantId);

        descriptors[descriptor.CombatantId] = descriptor;
        combatantsById[descriptor.CombatantId] = combatant;
        registrationOrder.Add(
            new KeyValuePair<string, TCombatant>(descriptor.CombatantId, combatant));

        if (descriptor.IsLocal)
        {
            localCombatantId = descriptor.CombatantId;
            localTeamId = descriptor.TeamId;
        }
    }

    public TCombatant Resolve(string combatantId)
    {
        if (string.IsNullOrEmpty(combatantId))
            return null;

        return combatantsById.TryGetValue(combatantId, out TCombatant combatant)
            ? combatant
            : null;
    }

    public string IdOf(TCombatant combatant)
    {
        if (combatant == null)
            return null;

        foreach (KeyValuePair<string, TCombatant> entry in registrationOrder)
        {
            if (ReferenceEquals(entry.Value, combatant) || entry.Value.Equals(combatant))
                return entry.Key;
        }
        return null;
    }

    public CombatantDescriptor DescriptorOf(string combatantId)
    {
        if (string.IsNullOrEmpty(combatantId))
            return null;

        return descriptors.TryGetValue(combatantId, out CombatantDescriptor descriptor)
            ? descriptor
            : null;
    }

    public bool IsLocalCombatant(string combatantId)
    {
        return localCombatantId != null
            && string.Equals(localCombatantId, combatantId, StringComparison.Ordinal);
    }

    public IReadOnlyList<TCombatant> Allies()
    {
        return ByTeam(sameTeam: true);
    }

    public IReadOnlyList<TCombatant> Opponents()
    {
        return ByTeam(sameTeam: false);
    }

    public void Clear()
    {
        descriptors.Clear();
        combatantsById.Clear();
        registrationOrder.Clear();
        localCombatantId = null;
        localTeamId = null;
    }

    private IReadOnlyList<TCombatant> ByTeam(bool sameTeam)
    {
        var result = new List<TCombatant>();
        if (localTeamId == null)
            return result;

        foreach (KeyValuePair<string, TCombatant> entry in registrationOrder)
        {
            CombatantDescriptor descriptor = descriptors[entry.Key];
            bool isSameTeam =
                string.Equals(descriptor.TeamId, localTeamId, StringComparison.Ordinal);
            if (isSameTeam == sameTeam)
                result.Add(entry.Value);
        }
        return result;
    }
}
