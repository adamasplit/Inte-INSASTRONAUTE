using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Another combatant's piles, as the server projects them: draw and hand as counts
/// only, discard and exhaust in full because both are public. Mirrors
/// StsPvpCombatView.StsPvpOpponentPiles.
/// </summary>
public sealed class RemotePiles<TCard> : ICombatantPiles<TCard> where TCard : class
{
    private readonly int drawCount;
    private readonly int handCount;
    private readonly List<TCard> discard;
    private readonly List<TCard> exhaust;

    public RemotePiles(
        int drawCount,
        int handCount,
        IEnumerable<TCard> discard,
        IEnumerable<TCard> exhaust)
    {
        this.drawCount = drawCount < 0 ? 0 : drawCount;
        this.handCount = handCount < 0 ? 0 : handCount;
        this.discard = discard == null ? new List<TCard>() : discard.ToList();
        this.exhaust = exhaust == null ? new List<TCard>() : exhaust.ToList();
    }

    public bool IsFullyVisible => false;

    public IList<TCard> Pile(PileKind kind)
    {
        switch (kind)
        {
            case PileKind.Discard: return discard;
            case PileKind.Exhaust: return exhaust;
            default: return null;   // draw and hand are counts only
        }
    }

    public int Count(PileKind kind)
    {
        switch (kind)
        {
            case PileKind.Draw: return drawCount;
            case PileKind.Hand: return handCount;
            case PileKind.Discard: return discard.Count;
            case PileKind.Exhaust: return exhaust.Count;
            default: return 0;
        }
    }

    public bool RemoveEverywhere(TCard card)
    {
        if (card == null)
            return false;

        bool removed = discard.Remove(card);
        return exhaust.Remove(card) || removed;
    }
}
