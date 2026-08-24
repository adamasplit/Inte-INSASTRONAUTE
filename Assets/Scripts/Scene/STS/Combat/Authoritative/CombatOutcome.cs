/// <summary>
/// How a combat ended, as the server settled it.
///
/// <para><c>Draw</c> has no equivalent in the local derivation from hit points, and it is
/// precisely the case that derivation read backwards: both teams wiped out gave "every enemy
/// is dead", and so a victory.</para>
/// </summary>
public enum CombatOutcome
{
    Undecided,
    Victory,
    Defeat,
    Draw
}
