using System;
using NUnit.Framework;

public class TurnCountdownTests
{
    private static readonly DateTimeOffset ServerNow =
        DateTimeOffset.Parse("2026-08-24T10:00:00Z");
    private static readonly DateTimeOffset ClientNow =
        DateTimeOffset.Parse("2026-08-24T10:00:00Z");

    private static TurnCountdown ThirtySecondTurn(DateTimeOffset receivedAt)
    {
        return TurnCountdown.FromState(
            ServerNow.AddSeconds(30).ToString("o"),
            ServerNow.ToString("o"),
            receivedAt);
    }

    [Test]
    public void AStateWithoutADeadlineIsNoCountdown()
    {
        TurnCountdown countdown = TurnCountdown.FromState(null, ServerNow.ToString("o"), ClientNow);

        Assert.That(countdown.HasDeadline, Is.False);
        Assert.That(countdown.SecondsRemainingAt(ClientNow), Is.Zero);
    }

    [Test]
    public void AFreshDeadlineReadsItsFullLength()
    {
        TurnCountdown countdown = ThirtySecondTurn(ClientNow);

        Assert.That(countdown.HasDeadline, Is.True);
        Assert.That(countdown.SecondsRemainingAt(ClientNow), Is.EqualTo(30d).Within(0.01));
    }

    [Test]
    public void TimePassingShortensIt()
    {
        TurnCountdown countdown = ThirtySecondTurn(ClientNow);

        Assert.That(countdown.SecondsRemainingAt(ClientNow.AddSeconds(12)),
            Is.EqualTo(18d).Within(0.01));
    }

    /// La raison d'être de serverTime : une horloge client fausse de dix minutes ne doit
    /// pas faire lire zéro seconde à un tour qui vient de commencer.
    [Test]
    public void AWrongClientClockDoesNotChangeTheRemainingTime()
    {
        DateTimeOffset skewedClientNow = ClientNow.AddMinutes(-10);
        TurnCountdown countdown = ThirtySecondTurn(skewedClientNow);

        Assert.That(countdown.SecondsRemainingAt(skewedClientNow),
            Is.EqualTo(30d).Within(0.01));
        Assert.That(countdown.SecondsRemainingAt(skewedClientNow.AddSeconds(25)),
            Is.EqualTo(5d).Within(0.01));
    }

    [Test]
    public void APastDeadlineReadsZeroRatherThanNegative()
    {
        TurnCountdown countdown = ThirtySecondTurn(ClientNow);

        Assert.That(countdown.SecondsRemainingAt(ClientNow.AddSeconds(45)), Is.Zero);
    }

    [Test]
    public void AnUnreadableTimestampIsNoCountdown()
    {
        Assert.That(TurnCountdown.FromState("bientôt", ServerNow.ToString("o"), ClientNow)
            .HasDeadline, Is.False);
        Assert.That(TurnCountdown.FromState(ServerNow.ToString("o"), "maintenant", ClientNow)
            .HasDeadline, Is.False);
    }
}
