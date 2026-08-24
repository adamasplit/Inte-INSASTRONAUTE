using System;
using System.Threading.Tasks;

/// <summary>
/// La preuve, répétée, qu'un joueur est encore devant son duel.
///
/// <para>Le serveur ne décide pas d'un forfait sur l'expiration d'un tour : il le décide sur
/// le silence. <c>StsPvpBattleTimeoutScheduler.forfeitTheAbsent</c> compare le dernier
/// battement de chaque participant à <c>now - disconnectGrace</c>, et le délai de grâce vaut
/// <b>120 secondes</b> (<c>app.sts.pvp.timeout.disconnect-grace-seconds</c>). Un participant
/// dont l'entrée n'existe pas encore est traité comme absent — d'où le premier battement
/// envoyé à l'ouverture du duel, et non un intervalle plus tard.</para>
///
/// <para>Le chemin serveur existe déjà de bout en bout : <c>POST /api/sts/pvp/battles/{id}/actions</c>
/// estampille la présence avant même de regarder ce que l'action demande, et un corps portant
/// <c>heartbeatOnly</c> ne joue rien. Ce qui manquait n'était que l'appelant.</para>
///
/// <para><b>Un battement raté n'interrompt rien.</b> Il se journalise, le suivant repart, et
/// le duel continue : arrêter un combat parce qu'une requête a échoué serait pire que le bug
/// qu'on corrige. C'est aussi pourquoi l'intervalle tient quatre fois dans la grâce — trois
/// échecs consécutifs passent encore.</para>
///
/// <para>Rien ici ne dépend d'un <c>MonoBehaviour</c> : le temps entre par
/// <see cref="AdvanceAsync"/>, l'envoi par le délégué du constructeur. La cadence, l'arrêt et
/// la tolérance à l'échec se testent donc en C# pur.</para>
/// </summary>
public sealed class PvpHeartbeat
{
    /// Le délai de grâce du serveur, en secondes. Copié ici pour que l'invariant de
    /// l'intervalle soit vérifiable sans lire le planificateur Java.
    public const double GraceSeconds = 120d;

    /// Combien d'échecs d'affilée l'intervalle doit pouvoir absorber avant que la grâce
    /// n'expire.
    public const int ToleratedConsecutiveFailures = 3;

    /// <summary>
    /// Vingt-cinq secondes, et non trente.
    ///
    /// <para>À trente, trois échecs mènent au battement de la 120e seconde, c'est-à-dire
    /// exactement à la limite : la moindre latence le fait arriver après. À vingt-cinq, le
    /// quatrième essai tombe à la centième seconde et garde vingt secondes de marge.</para>
    /// </summary>
    public const double DefaultIntervalSeconds = 25d;

    private readonly Func<Task<bool>> beat;
    private readonly Action<string> report;
    private readonly double intervalSeconds;

    private double sinceLastBeat;
    private bool beatInFlight;

    /// <param name="beat">
    /// L'envoi d'un battement. Rend <c>true</c> quand le serveur l'a reçu. Ni son résultat ni
    /// son exception ne remontent : les deux sont des échecs, et un échec ne fait rien d'autre
    /// que se journaliser.
    /// </param>
    /// <param name="report">Où va la mention d'un échec. Facultatif.</param>
    /// <param name="intervalSeconds">L'intervalle entre deux battements.</param>
    public PvpHeartbeat(
        Func<Task<bool>> beat,
        Action<string> report = null,
        double intervalSeconds = DefaultIntervalSeconds)
    {
        this.beat = beat ?? throw new ArgumentNullException(nameof(beat));
        this.report = report;

        if (intervalSeconds <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalSeconds), "A heartbeat interval must be a positive number of seconds");
        }

        // L'invariant qui donne son sens à la valeur : l'intervalle plus les échecs tolérés
        // doivent tenir dans la grâce, sinon un rythme trop lent perd le match sans rien dire.
        if (intervalSeconds * (ToleratedConsecutiveFailures + 1) > GraceSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalSeconds),
                $"A heartbeat every {intervalSeconds}s cannot absorb {ToleratedConsecutiveFailures} "
                + $"consecutive failures inside the {GraceSeconds}s grace period");
        }

        this.intervalSeconds = intervalSeconds;
    }

    /// Vrai tant que le duel est en cours et que des battements partent.
    public bool IsBeating { get; private set; }

    /// Combien de battements le serveur a acceptés.
    public int AcceptedCount { get; private set; }

    /// Combien ont échoué, depuis le début du duel.
    public int FailedCount { get; private set; }

    /// Combien ont échoué d'affilée. Remis à zéro par le premier qui passe.
    public int ConsecutiveFailures { get; private set; }

    /// <summary>
    /// Le duel commence.
    ///
    /// <para>Le premier battement part au prochain <see cref="AdvanceAsync"/>, sans attendre
    /// un intervalle : tant qu'aucun battement n'est arrivé, le serveur lit l'absence.</para>
    /// </summary>
    public void Begin()
    {
        if (IsBeating)
            return;

        IsBeating = true;
        AcceptedCount = 0;
        FailedCount = 0;
        ConsecutiveFailures = 0;
        sinceLastBeat = intervalSeconds;
    }

    /// <summary>
    /// Le duel est fini.
    ///
    /// <para>Plus aucun battement ne part ensuite. En entretenir un après coup afficherait au
    /// serveur une présence qui n'existe plus.</para>
    /// </summary>
    public void Stop()
    {
        IsBeating = false;
        sinceLastBeat = 0d;
    }

    /// <summary>
    /// Fait passer le temps, et envoie un battement s'il est dû.
    ///
    /// <para>La tâche rendue se termine avec le battement qu'elle a lancé — utile à un test,
    /// pas nécessaire à l'appelant : elle n'échoue jamais, et le compteur ne redémarre pas
    /// tant qu'un envoi est en vol.</para>
    /// </summary>
    public Task AdvanceAsync(double elapsedSeconds)
    {
        if (!IsBeating)
            return Task.CompletedTask;

        if (elapsedSeconds > 0d)
            sinceLastBeat += elapsedSeconds;

        // Un envoi encore en vol tient déjà lieu de battement : en lancer un second par-dessus
        // n'apprendrait rien au serveur et empilerait les requêtes d'un onglet lent.
        if (beatInFlight || sinceLastBeat < intervalSeconds)
            return Task.CompletedTask;

        sinceLastBeat = 0d;
        beatInFlight = true;
        return BeatOnceAsync();
    }

    private async Task BeatOnceAsync()
    {
        try
        {
            bool accepted = await beat().ConfigureAwait(false);
            if (accepted)
            {
                AcceptedCount++;
                ConsecutiveFailures = 0;
            }
            else
            {
                RecordFailure("the server did not acknowledge it");
            }
        }
        catch (Exception failure)
        {
            RecordFailure(failure.Message);
        }
        finally
        {
            beatInFlight = false;
        }
    }

    private void RecordFailure(string reason)
    {
        FailedCount++;
        ConsecutiveFailures++;

        double silentFor = ConsecutiveFailures * intervalSeconds;
        string warning =
            $"[STS-PVP] Heartbeat {ConsecutiveFailures} in a row failed ({reason}). "
            + $"Roughly {silentFor:0}s of the {GraceSeconds:0}s grace period spent; the duel goes on.";
        report?.Invoke(warning);
    }
}
