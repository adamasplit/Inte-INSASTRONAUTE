using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

/// <summary>
/// Ce qu'on peut prouver ici : la cadence, l'arrêt, et le fait qu'un échec n'arrête rien.
///
/// <para>Ce qu'on ne peut pas : le bridage des minuteries d'un onglet en arrière-plan. Il se
/// mesure avec deux téléphones, il ne se déduit pas d'un test.</para>
/// </summary>
public class PvpHeartbeatTests
{
    /// Un envoi qu'on pilote : il compte les appels et rend ce qu'on lui dit de rendre.
    private sealed class FakeBeat
    {
        private readonly Queue<Func<bool>> answers = new Queue<Func<bool>>();

        public int Calls { get; private set; }

        public bool NextAnswer { get; set; } = true;

        public void AnswerOnce(Func<bool> answer) => answers.Enqueue(answer);

        public Task<bool> SendAsync()
        {
            Calls++;
            if (answers.Count > 0)
                return Task.FromResult(answers.Dequeue()());
            return Task.FromResult(NextAnswer);
        }
    }

    private static PvpHeartbeat Beating(FakeBeat beat, List<string> log = null)
    {
        var heartbeat = new PvpHeartbeat(beat.SendAsync, log != null ? log.Add : (Action<string>)null);
        heartbeat.Begin();
        return heartbeat;
    }

    /// Un participant sans battement enregistré est déjà lu comme absent par le serveur : le
    /// premier ne peut donc pas attendre un intervalle.
    [Test]
    public async Task TheFirstBeatLeavesAsSoonAsTheDuelOpens()
    {
        var beat = new FakeBeat();
        PvpHeartbeat heartbeat = Beating(beat);

        await heartbeat.AdvanceAsync(0d);

        Assert.That(beat.Calls, Is.EqualTo(1));
        Assert.That(heartbeat.AcceptedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task NothingLeavesBeforeTheDuelHasBegun()
    {
        var beat = new FakeBeat();
        var heartbeat = new PvpHeartbeat(beat.SendAsync);

        await heartbeat.AdvanceAsync(300d);

        Assert.That(beat.Calls, Is.Zero);
        Assert.That(heartbeat.IsBeating, Is.False);
    }

    [Test]
    public async Task ItBeatsOncePerIntervalAndNotMore()
    {
        var beat = new FakeBeat();
        PvpHeartbeat heartbeat = Beating(beat);

        // Le premier battement, puis deux minutes de duel une seconde à la fois.
        await heartbeat.AdvanceAsync(0d);
        for (int second = 0; second < 120; second++)
            await heartbeat.AdvanceAsync(1d);

        // 120 s au rythme de 25 s : celui de l'ouverture, puis 25, 50, 75, 100.
        Assert.That(beat.Calls, Is.EqualTo(5));
    }

    [Test]
    public async Task ATickShorterThanTheIntervalSendsNothing()
    {
        var beat = new FakeBeat();
        PvpHeartbeat heartbeat = Beating(beat);
        await heartbeat.AdvanceAsync(0d);

        await heartbeat.AdvanceAsync(24d);

        Assert.That(beat.Calls, Is.EqualTo(1));
    }

    /// La raison d'être du rythme choisi : trois échecs d'affilée doivent laisser au quatrième
    /// essai le temps d'arriver avant la fin de la grâce.
    [Test]
    public void TheIntervalAbsorbsThreeConsecutiveFailuresInsideTheGrace()
    {
        Assert.That(
            PvpHeartbeat.DefaultIntervalSeconds * (PvpHeartbeat.ToleratedConsecutiveFailures + 1),
            Is.LessThanOrEqualTo(PvpHeartbeat.GraceSeconds));
    }

    [Test]
    public void AnIntervalTooSlowForTheGraceIsRefusedOutright()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PvpHeartbeat(() => Task.FromResult(true), null, 31d));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PvpHeartbeat(() => Task.FromResult(true), null, 0d));
    }

    [Test]
    public async Task ARefusedBeatIsLoggedAndTheNextOneLeavesAnyway()
    {
        var log = new List<string>();
        var beat = new FakeBeat { NextAnswer = false };
        PvpHeartbeat heartbeat = Beating(beat, log);

        await heartbeat.AdvanceAsync(0d);
        Assert.That(heartbeat.ConsecutiveFailures, Is.EqualTo(1));
        Assert.That(log, Has.Count.EqualTo(1));

        beat.NextAnswer = true;
        await heartbeat.AdvanceAsync(25d);

        Assert.That(beat.Calls, Is.EqualTo(2));
        Assert.That(heartbeat.AcceptedCount, Is.EqualTo(1));
        Assert.That(heartbeat.FailedCount, Is.EqualTo(1));
        Assert.That(heartbeat.ConsecutiveFailures, Is.Zero);
    }

    /// Le point entier de la tolérance : une exception de transport ne doit pas remonter à
    /// l'appelant, sans quoi un duel s'arrêterait parce qu'une requête a échoué.
    [Test]
    public async Task AThrowingBeatNeverThrowsBackAtTheCaller()
    {
        var log = new List<string>();
        var beat = new FakeBeat();
        beat.AnswerOnce(() => throw new InvalidOperationException("bridge is asleep"));
        PvpHeartbeat heartbeat = Beating(beat, log);

        Assert.DoesNotThrowAsync(async () => await heartbeat.AdvanceAsync(0d));

        Assert.That(heartbeat.FailedCount, Is.EqualTo(1));
        Assert.That(log[0], Does.Contain("bridge is asleep"));
        Assert.That(heartbeat.IsBeating, Is.True);

        await heartbeat.AdvanceAsync(25d);
        Assert.That(beat.Calls, Is.EqualTo(2));
        Assert.That(heartbeat.AcceptedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ThreeFailuresInARowStillLeaveAFourthTryInsideTheGrace()
    {
        var log = new List<string>();
        var beat = new FakeBeat { NextAnswer = false };
        PvpHeartbeat heartbeat = Beating(beat, log);

        double elapsed = 0d;
        await heartbeat.AdvanceAsync(0d);
        for (int failure = 0; failure < 2; failure++)
        {
            await heartbeat.AdvanceAsync(PvpHeartbeat.DefaultIntervalSeconds);
            elapsed += PvpHeartbeat.DefaultIntervalSeconds;
        }

        Assert.That(heartbeat.ConsecutiveFailures, Is.EqualTo(3));

        beat.NextAnswer = true;
        await heartbeat.AdvanceAsync(PvpHeartbeat.DefaultIntervalSeconds);
        elapsed += PvpHeartbeat.DefaultIntervalSeconds;

        Assert.That(heartbeat.AcceptedCount, Is.EqualTo(1));
        Assert.That(elapsed, Is.LessThan(PvpHeartbeat.GraceSeconds));
    }

    [Test]
    public async Task StoppingEndsTheBeatingForGood()
    {
        var beat = new FakeBeat();
        PvpHeartbeat heartbeat = Beating(beat);
        await heartbeat.AdvanceAsync(0d);

        heartbeat.Stop();
        await heartbeat.AdvanceAsync(600d);

        Assert.That(heartbeat.IsBeating, Is.False);
        Assert.That(beat.Calls, Is.EqualTo(1));
    }

    /// Un envoi encore en vol ne doit pas en déclencher un second par-dessus : ce serait empiler
    /// des requêtes sur un onglet déjà lent, sans rien apprendre au serveur.
    [Test]
    public async Task NoSecondBeatLeavesWhileTheFirstIsStillInFlight()
    {
        var pending = new TaskCompletionSource<bool>();
        int calls = 0;
        var heartbeat = new PvpHeartbeat(() =>
        {
            calls++;
            return pending.Task;
        });
        heartbeat.Begin();

        Task inFlight = heartbeat.AdvanceAsync(0d);
        await heartbeat.AdvanceAsync(100d);

        Assert.That(calls, Is.EqualTo(1));

        pending.SetResult(true);
        await inFlight;
        Assert.That(heartbeat.AcceptedCount, Is.EqualTo(1));
    }

    /// Ce que « l'émission s'arrête à la fin du duel » veut dire quand un envoi traîne : celui
    /// qui est parti finit, mais aucun autre ne part.
    [Test]
    public async Task StoppingWhileABeatIsInFlightStartsNoOtherOne()
    {
        var pending = new TaskCompletionSource<bool>();
        int calls = 0;
        var heartbeat = new PvpHeartbeat(() =>
        {
            calls++;
            return pending.Task;
        });
        heartbeat.Begin();

        Task inFlight = heartbeat.AdvanceAsync(0d);
        heartbeat.Stop();
        pending.SetResult(true);
        await inFlight;

        await heartbeat.AdvanceAsync(100d);

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(heartbeat.IsBeating, Is.False);
    }

    [Test]
    public async Task BeginningAgainStartsANewDuelsWorthOfCounters()
    {
        var beat = new FakeBeat { NextAnswer = false };
        PvpHeartbeat heartbeat = Beating(beat);
        await heartbeat.AdvanceAsync(0d);
        heartbeat.Stop();

        beat.NextAnswer = true;
        heartbeat.Begin();

        Assert.That(heartbeat.FailedCount, Is.Zero);
        Assert.That(heartbeat.ConsecutiveFailures, Is.Zero);

        await heartbeat.AdvanceAsync(0d);
        Assert.That(heartbeat.AcceptedCount, Is.EqualTo(1));
    }

    [Test]
    public void ASendDelegateIsRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new PvpHeartbeat(null));
    }
}
