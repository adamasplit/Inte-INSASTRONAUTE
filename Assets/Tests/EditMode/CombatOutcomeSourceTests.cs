using NUnit.Framework;

public class CombatOutcomeSourceTests
{
    [Test]
    public void TheWinningTeamBeingOursIsAVictory()
    {
        Assert.AreEqual(CombatOutcome.Victory,
            CombatOutcomeSource.FromWinner("players", "players"));
    }

    [Test]
    public void AnotherTeamWinningIsADefeat()
    {
        Assert.AreEqual(CombatOutcome.Defeat,
            CombatOutcomeSource.FromWinner("enemies", "players"));
    }

    /// A combat that ended with no winner is a draw, and that is the only reason the server
    /// names none: CombatEnded is emitted on a finished combat and on nothing else.
    [Test]
    public void NoWinningTeamIsADraw()
    {
        Assert.AreEqual(CombatOutcome.Draw, CombatOutcomeSource.FromWinner(null, "players"));
        Assert.AreEqual(CombatOutcome.Draw, CombatOutcomeSource.FromWinner("", "players"));
        Assert.AreEqual(CombatOutcome.Draw, CombatOutcomeSource.FromWinner("   ", "players"));
    }

    /// Not knowing which team we are on decides nothing -- least of all a defeat.
    [Test]
    public void WithoutOurOwnTeamNothingIsDecided()
    {
        Assert.AreEqual(CombatOutcome.Undecided, CombatOutcomeSource.FromWinner("players", null));
        Assert.AreEqual(CombatOutcome.Undecided, CombatOutcomeSource.FromWinner("players", ""));
    }

    /// Team ids are opaque and compared byte for byte: a difference of case is another team,
    /// not ours spelled differently.
    [Test]
    public void TeamIdsAreComparedExactly()
    {
        Assert.AreEqual(CombatOutcome.Defeat,
            CombatOutcomeSource.FromWinner("Players", "players"));
    }
}
