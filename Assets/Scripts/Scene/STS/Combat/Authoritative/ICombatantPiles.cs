using System.Collections.Generic;

/// <summary>
/// One combatant's four piles.
///
/// <para>Two implementations exist because the server shows two different things: our
/// own piles in full, and another combatant's reduced to counts plus the two public
/// piles. <see cref="Pile"/> returns null for a pile this viewer is not allowed to
/// see, which is deliberately different from an empty list.</para>
/// </summary>
public interface ICombatantPiles<TCard> where TCard : class
{
    /// <summary>True when every pile is readable as a list.</summary>
    bool IsFullyVisible { get; }

    /// <summary>The cards in that pile, or null when they are not ours to see.</summary>
    IList<TCard> Pile(PileKind kind);

    /// <summary>How many cards that pile holds — known even when the cards are not.</summary>
    int Count(PileKind kind);

    /// <summary>
    /// Removes the card from whichever visible pile holds it. Returns false when no
    /// visible pile held it, which includes the case where it sits in a hidden one.
    /// </summary>
    bool RemoveEverywhere(TCard card);
}
