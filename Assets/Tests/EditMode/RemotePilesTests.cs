using System.Collections.Generic;
using NUnit.Framework;

public class RemotePilesTests
{
    private static RemotePiles<string> OpponentPiles()
    {
        return new RemotePiles<string>(
            drawCount: 7,
            handCount: 3,
            discard: new[] { "burned", "spent" },
            exhaust: new[] { "gone" });
    }

    [Test]
    public void AnnouncesThatItIsNotFullyVisible()
    {
        Assert.That(OpponentPiles().IsFullyVisible, Is.False);
    }

    /// <summary>
    /// The server sends the opponent's draw and hand as counts only, so there is no
    /// list to hand back. Returning null rather than an empty list keeps "you may not
    /// see this" distinguishable from "this is empty". Cf. spec §4.3.
    /// </summary>
    [Test]
    public void HidesTheCardsItWasNeverGiven()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.Pile(PileKind.Draw), Is.Null);
        Assert.That(piles.Pile(PileKind.Hand), Is.Null);
    }

    [Test]
    public void CountsTheHiddenPilesItWasGivenNumbersFor()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.Count(PileKind.Draw), Is.EqualTo(7));
        Assert.That(piles.Count(PileKind.Hand), Is.EqualTo(3));
    }

    [Test]
    public void ShowsThePublicPilesInFull()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.Pile(PileKind.Discard),
            Is.EqualTo(new[] { "burned", "spent" }));
        Assert.That(piles.Pile(PileKind.Exhaust), Is.EqualTo(new[] { "gone" }));
        Assert.That(piles.Count(PileKind.Discard), Is.EqualTo(2));
    }

    [Test]
    public void RemovesFromThePublicPilesOnly()
    {
        RemotePiles<string> piles = OpponentPiles();

        Assert.That(piles.RemoveEverywhere("burned"), Is.True);
        Assert.That(piles.Pile(PileKind.Discard), Is.EqualTo(new[] { "spent" }));

        // A card we cannot see cannot be removed, and saying so is the point.
        Assert.That(piles.RemoveEverywhere("never-seen"), Is.False);
    }

    [Test]
    public void TreatsMissingListsAsEmptyRatherThanThrowing()
    {
        var piles = new RemotePiles<string>(0, 0, null, null);

        Assert.That(piles.Pile(PileKind.Discard), Is.Empty);
        Assert.That(piles.Count(PileKind.Exhaust), Is.Zero);
    }
}
