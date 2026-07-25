public enum STSRunResumePhase
{
    Map,
    Combat,
    Event,
    Rest,
    Reward,
    Retreat
}

public static class STSRunResumeResolver
{
    public static STSRunResumePhase Resolve(
        bool hasActiveEncounter,
        bool hasActiveEvent,
        string enteredNodeType,
        bool hasPendingRewards,
        string currentNodeType,
        bool currentNodeCompleted)
    {
        if (hasActiveEncounter)
            return STSRunResumePhase.Combat;

        if (hasActiveEvent && string.Equals(enteredNodeType, "Event", System.StringComparison.OrdinalIgnoreCase))
            return STSRunResumePhase.Event;

        if (string.Equals(enteredNodeType, "Rest", System.StringComparison.OrdinalIgnoreCase))
            return STSRunResumePhase.Rest;

        if (hasPendingRewards)
            return STSRunResumePhase.Reward;

        if (currentNodeCompleted
            && string.Equals(currentNodeType, "Boss", System.StringComparison.OrdinalIgnoreCase))
            return STSRunResumePhase.Retreat;

        return STSRunResumePhase.Map;
    }
}
