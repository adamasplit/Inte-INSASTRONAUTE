using System;

public enum CombatantController
{
    Human,
    Ai
}

/// <summary>
/// What the server says a combatant is, independently of how it is drawn: who it is,
/// which team it fights for, who drives it, and whether it is ours.
/// </summary>
public sealed class CombatantDescriptor
{
    public CombatantDescriptor(
        string combatantId,
        string teamId,
        CombatantController controller,
        bool isLocal)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
            throw new ArgumentException("A combatant needs an id", nameof(combatantId));
        if (string.IsNullOrWhiteSpace(teamId))
            throw new ArgumentException("A combatant needs a team", nameof(teamId));

        CombatantId = combatantId;
        TeamId = teamId;
        Controller = controller;
        IsLocal = isLocal;
    }

    public string CombatantId { get; }
    public string TeamId { get; }
    public CombatantController Controller { get; }
    public bool IsLocal { get; }
}
