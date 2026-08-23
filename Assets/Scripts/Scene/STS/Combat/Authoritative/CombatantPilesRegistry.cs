using System;
using System.Collections.Generic;

/// <summary>
/// Which piles belong to which combatant.
///
/// <para>Separate from <c>CombatantRegistry</c> on purpose: identity is settled once
/// at setup and never moves, whereas piles are replaced by every state that arrives.
/// Holding no piles for a combatant is normal — a PvE enemy has none — so
/// <see cref="For"/> answers null rather than inventing an empty set.</para>
/// </summary>
public sealed class CombatantPilesRegistry<TCard> where TCard : class
{
    private readonly Dictionary<string, ICombatantPiles<TCard>> pilesByCombatant =
        new Dictionary<string, ICombatantPiles<TCard>>(StringComparer.Ordinal);

    public void Set(string combatantId, ICombatantPiles<TCard> piles)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
            throw new ArgumentException("Piles need an owner", nameof(combatantId));
        if (piles == null)
            throw new ArgumentNullException(nameof(piles));

        pilesByCombatant[combatantId] = piles;
    }

    public ICombatantPiles<TCard> For(string combatantId)
    {
        if (string.IsNullOrEmpty(combatantId))
            return null;

        return pilesByCombatant.TryGetValue(combatantId, out ICombatantPiles<TCard> piles)
            ? piles
            : null;
    }

    /// <summary>
    /// Convenience for the common read. Null means either "no such combatant" or
    /// "that pile is not ours to see"; both call for skipping, not guessing.
    /// </summary>
    public IList<TCard> Pile(string combatantId, PileKind kind)
    {
        return For(combatantId)?.Pile(kind);
    }

    public void Clear()
    {
        pilesByCombatant.Clear();
    }
}
