public class UndeadStatus : StatusEffect
{
    public UndeadStatus(int duration)
    {
        Duration = duration;
        Name = "Zombie";
        debuff = true;
        generic = true;
        framed=true;
    }
    public override void OnHeal(Character target, ref int healAmount)
    {
        if (healAmount <= 0)
            return;
        int converted = healAmount;
        healAmount = 0;
        target.TakeDamage(converted, true);
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Les soins que vous recevez sont convertis en perte de PV.";
        }
        return $"Les soins que reçoit le personnage sont convertis en perte de PV.";
    }
}
