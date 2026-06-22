public class StatTypeChecker
{
    public static bool IsValid(StatType stat)
    {
        return stat!=StatType.TurnDelay && stat != StatType.Cost && stat != StatType.ReplayCount&& stat != StatType.Any;
    }
}