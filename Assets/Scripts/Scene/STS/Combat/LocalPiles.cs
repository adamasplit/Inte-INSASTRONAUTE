using System.Collections.Generic;

/// <summary>
/// The local player's piles, which are the DeckManager's own lists.
///
/// <para>This is an adapter and nothing more: it hands back the very lists the
/// DeckManager holds, so the hand UI, the animations and the OnCard* events keep
/// working off the same objects they always did. It lives outside
/// STS.AuthoritativeCombat because DeckManager and CardInstance both reference the
/// Unity engine.</para>
/// </summary>
public sealed class LocalPiles : ICombatantPiles<CardInstance>
{
    private readonly DeckManager deck;

    public LocalPiles(DeckManager deck)
    {
        this.deck = deck;
    }

    public bool IsFullyVisible => true;

    public IList<CardInstance> Pile(PileKind kind)
    {
        if (deck == null)
            return null;

        switch (kind)
        {
            case PileKind.Draw: return deck.drawPile;
            case PileKind.Hand: return deck.hand;
            case PileKind.Discard: return deck.discardPile;
            case PileKind.Exhaust: return deck.exhaustPile;
            default: return null;
        }
    }

    public int Count(PileKind kind)
    {
        IList<CardInstance> pile = Pile(kind);
        return pile == null ? 0 : pile.Count;
    }

    public bool RemoveEverywhere(CardInstance card)
    {
        if (deck == null || card == null)
            return false;

        bool removed = deck.hand.Remove(card);
        removed |= deck.drawPile.Remove(card);
        removed |= deck.discardPile.Remove(card);
        removed |= deck.exhaustPile.Remove(card);
        return removed;
    }
}
