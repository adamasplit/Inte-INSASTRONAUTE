using System.Collections.Generic;
using NUnit.Framework;

public class CombatantPilesRegistryTests
{
    private static CombatantPilesRegistry<string> TwoCombatants()
    {
        var registry = new CombatantPilesRegistry<string>();
        registry.Set("player", new RemotePiles<string>(
            drawCount: 5, handCount: 2,
            discard: new[] { "mine-discarded" }, exhaust: null));
        registry.Set("enemy-0", new RemotePiles<string>(
            drawCount: 9, handCount: 4,
            discard: new[] { "theirs-discarded" }, exhaust: null));
        return registry;
    }

    [Test]
    public void KeepsEachCombatantsPilesApart()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        Assert.That(registry.Pile("player", PileKind.Discard),
            Is.EqualTo(new[] { "mine-discarded" }));
        Assert.That(registry.Pile("enemy-0", PileKind.Discard),
            Is.EqualTo(new[] { "theirs-discarded" }));
    }

    [Test]
    public void CountsBelongToTheirOwner()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        Assert.That(registry.For("player").Count(PileKind.Draw), Is.EqualTo(5));
        Assert.That(registry.For("enemy-0").Count(PileKind.Draw), Is.EqualTo(9));
    }

    /// <summary>
    /// A combatant we hold no piles for is the normal PvE case for an enemy. Answering
    /// null lets the caller skip the event instead of writing into someone else's
    /// deck, which is what the single deck did. Cf. spec §4.3.
    /// </summary>
    [Test]
    public void ReturnsNullForACombatantItHoldsNoPilesFor()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        Assert.That(registry.For("enemy-1"), Is.Null);
        Assert.That(registry.Pile("enemy-1", PileKind.Hand), Is.Null);
        Assert.That(registry.For(null), Is.Null);
    }

    [Test]
    public void ReplacesPilesWhenAFresherStateArrives()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        registry.Set("player", new RemotePiles<string>(
            drawCount: 1, handCount: 0, discard: null, exhaust: null));

        Assert.That(registry.For("player").Count(PileKind.Draw), Is.EqualTo(1));
        Assert.That(registry.Pile("player", PileKind.Discard), Is.Empty);
    }

    [Test]
    public void ForgetsEverythingOnClear()
    {
        CombatantPilesRegistry<string> registry = TwoCombatants();

        registry.Clear();

        Assert.That(registry.For("player"), Is.Null);
    }
}
