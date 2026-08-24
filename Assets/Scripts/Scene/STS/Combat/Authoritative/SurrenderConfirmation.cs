using System;

/// <summary>
/// Le verrou entre le bouton d'abandon et l'abandon lui-même.
///
/// <para>Abandonner est irréversible et coûte le classement : <c>StsPvpCombatService.concede</c>
/// appelle <c>moveTheRating</c> exactement comme le fait le forfait d'un joueur absent. Un clic
/// de trop qui perd un match classé serait un défaut plus grave que l'absence de bouton — d'où
/// deux pressions, la seconde dans une fenêtre courte, et un texte qui dit ce que ça coûte.</para>
///
/// <para>La fenêtre se referme seule : une confirmation armée puis oubliée ne doit pas transformer
/// en abandon la pression que le joueur fera dix minutes plus tard sur le même bouton.</para>
///
/// <para>Aucune dépendance à Unity : le temps entre par <see cref="Advance"/>, ce qui rend la
/// question « ce clic abandonne-t-il ? » testable en C# pur.</para>
/// </summary>
public sealed class SurrenderConfirmation
{
    /// Combien de temps la confirmation reste offerte après la première pression.
    public const double DefaultWindowSeconds = 8d;

    /// Ce que le bouton dit tant que rien n'est armé.
    public const string IdleLabel = "Abandonner";

    /// Ce qu'il dit une fois armé. La seconde pression est celle qui abandonne, et il faut
    /// qu'elle se lise comme telle.
    public const string ArmedLabel = "Confirmer l'abandon";

    /// <summary>
    /// L'avertissement, qui ne prend pas de gants : le serveur traite l'abandon volontaire
    /// comme une absence, classement compris. Le taire ferait croire à une sortie gratuite.
    /// </summary>
    public const string Warning =
        "Abandonner met fin au duel : votre adversaire gagne, et votre classement baisse "
        + "exactement comme si vous aviez quitté la partie. Appuyez une seconde fois pour "
        + "confirmer.";

    private readonly double windowSeconds;
    private double armedFor;

    public SurrenderConfirmation(double windowSeconds = DefaultWindowSeconds)
    {
        if (windowSeconds <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowSeconds), "A confirmation window must be a positive number of seconds");
        }

        this.windowSeconds = windowSeconds;
    }

    /// Vrai quand la prochaine pression abandonnerait.
    public bool IsArmed { get; private set; }

    /// Le texte que le bouton doit porter, dans l'état où il est.
    public string Label => IsArmed ? ArmedLabel : IdleLabel;

    /// <summary>
    /// Le joueur a appuyé.
    /// </summary>
    /// <returns>
    /// <c>true</c> seulement à la seconde pression, dans la fenêtre : c'est le seul cas où
    /// l'abandon doit partir. La première arme la confirmation et ne rend rien.
    /// </returns>
    public bool Press()
    {
        if (IsArmed)
        {
            Reset();
            return true;
        }

        IsArmed = true;
        armedFor = 0d;
        return false;
    }

    /// Le joueur s'est ravisé, ou le duel s'est terminé pendant qu'il hésitait.
    public void Reset()
    {
        IsArmed = false;
        armedFor = 0d;
    }

    /// <summary>
    /// Fait passer le temps. Une confirmation restée armée au-delà de la fenêtre se désarme
    /// d'elle-même.
    /// </summary>
    public void Advance(double elapsedSeconds)
    {
        if (!IsArmed || elapsedSeconds <= 0d)
            return;

        armedFor += elapsedSeconds;
        if (armedFor >= windowSeconds)
            Reset();
    }

    /// Combien de secondes il reste pour confirmer. Zéro quand rien n'est armé.
    public double SecondsLeftToConfirm => IsArmed ? Math.Max(0d, windowSeconds - armedFor) : 0d;
}
