public class LamesArdentesStatus : StatusEffect
{
    public LamesArdentesStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Lames ardentes";
        buff = true;
        framed = true;
    }
    public override void OnDamageDealt(Character source, Character target, ref int damage)
    {
        if (damage > 0)
        {
            target.AddStatus(new BurnStatus(Value));
        }
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
            return $"Chaque fois que vous infligez des dégâts non-bloqués, appliquez {Value} de Brûlure.";
        return $"Chaque fois que le personnage inflige des dégâts non-bloqués, sa cible subit {Value} de Brûlure.";
    }
}
