using System;

/// <summary>
/// Who won, read rather than deduced.
///
/// <para>The client used to derive the outcome from the hit points it could see. That holds
/// only while a combat ends because somebody died and while the client owns those hit points
/// -- two assumptions PvP breaks outright (a forfeit closes a combat with nobody dead) and
/// that PvE already broke: two teams wiped out gave a victory where the server had recorded a
/// draw.</para>
///
/// <para>To be asked of a <c>CombatEnded</c> and of nothing else: there alone does a missing
/// <c>winnerTeamId</c> mean "drawn" rather than "not finished yet".</para>
/// </summary>
public static class CombatOutcomeSource
{
    public static CombatOutcome FromWinner(string winnerTeamId, string localTeamId)
    {
        if (string.IsNullOrWhiteSpace(localTeamId))
            return CombatOutcome.Undecided;

        if (string.IsNullOrWhiteSpace(winnerTeamId))
            return CombatOutcome.Draw;

        return string.Equals(winnerTeamId, localTeamId, StringComparison.Ordinal)
            ? CombatOutcome.Victory
            : CombatOutcome.Defeat;
    }
}
