public class ContinuousStatus : StatusEffect
{
    public ContinuousStatus(int value, int duration)
    {
        this.Value = value;
        this.Duration = duration;
        Name = "Milieu continu";
        debuff=true;
    }
    public override void OnTurnEnd(Character target){}
    public override void OnFieldTurnEnd(Character target)
    {
        Tick(target);
        target.TakeDamage(this.Value);
        VFXManager.Instance.PlayEffect("Continuous", target);
    }
    public override string Desc()
    {
        return $"\nLa cible subit {Value} dégâts à la fin du tour de n'importe quel personnage. (s'active {Duration} fois).";
    }
}