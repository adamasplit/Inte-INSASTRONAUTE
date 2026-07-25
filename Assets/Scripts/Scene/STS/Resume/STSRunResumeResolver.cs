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

public static class STSRestState
{
    public static int InitialCharges(bool isEnteredRest, int currentCharges, int maxCharges)
    {
        return isEnteredRest ? System.Math.Max(0, maxCharges) : System.Math.Max(0, currentCharges);
    }
}

public static class STSHealing
{
    public static int Apply(int currentHp, int maxHp, int amount)
    {
        return System.Math.Min(maxHp, currentHp + amount);
    }
}

public sealed class STSCompletionGate
{
    private bool isRunning;

    public bool TryBegin()
    {
        if (isRunning)
            return false;

        isRunning = true;
        return true;
    }

    public void Reset()
    {
        isRunning = false;
    }
}
