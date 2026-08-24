using System;
using System.Globalization;

/// <summary>
/// Combien de temps il reste au tour en cours, mesuré sur l'horloge du serveur.
///
/// <para>Un tour PvP dure trente secondes et se perd sans prévenir. Le compte à rebours
/// ne peut pas se calculer sur l'heure locale : elle peut être fausse de plusieurs
/// minutes, et le joueur verrait alors un tour déjà expiré ou éternel. La vue porte donc
/// <c>serverTime</c> à côté de <c>turnDeadline</c> ; l'écart entre cette heure-là et
/// celle qu'il était ici quand le message est arrivé est le décalage qu'on applique
/// ensuite à chaque lecture.</para>
///
/// <para>Une structure sans deadline est la réponse normale, pas une erreur : le PvE
/// n'envoie aucun de ces deux champs, et rien ne doit s'afficher.</para>
/// </summary>
public readonly struct TurnCountdown
{
    private readonly DateTimeOffset deadline;
    private readonly TimeSpan clockOffset;

    private TurnCountdown(DateTimeOffset deadline, TimeSpan clockOffset)
    {
        this.deadline = deadline;
        this.clockOffset = clockOffset;
        HasDeadline = true;
    }

    public static TurnCountdown None => default;

    public bool HasDeadline { get; }

    public static TurnCountdown FromState(
        string turnDeadline,
        string serverTime,
        DateTimeOffset receivedAt)
    {
        if (!TryParse(turnDeadline, out DateTimeOffset parsedDeadline))
            return None;
        if (!TryParse(serverTime, out DateTimeOffset parsedServerTime))
            return None;

        return new TurnCountdown(parsedDeadline, parsedServerTime - receivedAt);
    }

    /// <summary>
    /// Les secondes restantes, jamais négatives : un tour expiré vaut zéro, et c'est le
    /// serveur qui dira ce qu'il advient de lui.
    /// </summary>
    public double SecondsRemainingAt(DateTimeOffset now)
    {
        if (!HasDeadline)
            return 0d;

        double remaining = (deadline - (now + clockOffset)).TotalSeconds;
        return remaining < 0d ? 0d : remaining;
    }

    private static bool TryParse(string value, out DateTimeOffset parsed)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out parsed);
    }
}
