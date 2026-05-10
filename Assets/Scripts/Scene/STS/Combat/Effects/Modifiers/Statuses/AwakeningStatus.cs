public class AwakeningStatus : StatusEffect
{
    public AwakeningStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Éveil";
        buff=true;
    }
    public override void OnExpire(Character target)
    {
        base.OnExpire(target);
        target.Heal(Value);
    }
    public override string Desc()
    {
        return $"\nSoigne {Value} PV à l'expiration";
    }
}