using System.Linq;
public class SublimationStatus : StatusEffect
{
    public SublimationStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Sublimation";
        buff = true;
        framed = true;
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        if (damage <= 0)
        {
            return;
        }
        int burn = target.statusEffects.OfType<BurnStatus>().Sum(status => status.Duration);
        if (burn >= 10)
        {
            source.Heal(Value);
        }
    }
    public override string Desc(bool isPlayer)
    {
        return $"Quand vous infligez des dégâts à un ennemi qui a 10 ou plus de Brûlure, regagnez {Value} PV.";
    }
}
