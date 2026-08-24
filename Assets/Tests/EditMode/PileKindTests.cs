using NUnit.Framework;

public class PileKindTests
{
    [Test]
    public void ParsesTheFourNamesTheServerEmits()
    {
        Assert.That(PileKinds.Parse("HAND"), Is.EqualTo(PileKind.Hand));
        Assert.That(PileKinds.Parse("DRAW"), Is.EqualTo(PileKind.Draw));
        Assert.That(PileKinds.Parse("DISCARD"), Is.EqualTo(PileKind.Discard));
        Assert.That(PileKinds.Parse("EXHAUST"), Is.EqualTo(PileKind.Exhaust));
    }

    /// <summary>
    /// The server emits upper case exactly, but tolerating case costs nothing and
    /// removes a whole class of untraceable mismatch. Tolerating *substrings* is what
    /// we refuse: cf. spec §3.4 entry 8.
    /// </summary>
    [Test]
    public void ToleratesCaseAndSurroundingWhitespaceOnly()
    {
        Assert.That(PileKinds.Parse("hand"), Is.EqualTo(PileKind.Hand));
        Assert.That(PileKinds.Parse("  DRAW  "), Is.EqualTo(PileKind.Draw));
    }

    [Test]
    public void RefusesASpellingTheServerHasNeverEmitted()
    {
        // The old ResolvePileName answered Draw to all three of these, by substring.
        Assert.That(PileKinds.Parse("DRAW_PILE"), Is.Null);
        Assert.That(PileKinds.Parse("DECK"), Is.Null);
        Assert.That(PileKinds.Parse("THE HAND OF FATE"), Is.Null);
    }

    [Test]
    public void RefusesNothingness()
    {
        Assert.That(PileKinds.Parse(null), Is.Null);
        Assert.That(PileKinds.Parse(""), Is.Null);
        Assert.That(PileKinds.Parse("   "), Is.Null);
    }

    [Test]
    public void RoundTripsThroughTheWireName()
    {
        Assert.That(PileKinds.ToWireName(PileKind.Hand), Is.EqualTo("HAND"));
        Assert.That(PileKinds.Parse(PileKinds.ToWireName(PileKind.Exhaust)),
            Is.EqualTo(PileKind.Exhaust));
    }
}
