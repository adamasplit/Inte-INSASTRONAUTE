public class AwakeningStatus : StatusEffect
{
    public AwakeningStatus(int value, int duration)
    {
        Value = value;
        Duration = duration;
        Name = "Éveil";
        buff=true;
    }
    public override void Merge(StatusEffect other)
    {
        other.Duration += this.Duration;
        other.Value = this.Value+other.Value;
    }
    public override void OnExpire(Character target)
    {
        base.OnExpire(target);
        target.Heal(Value);
    }
    public override string Desc()
    {
        return $"Soigne {Value} PV au bout de {Duration} tours.";
    }
}