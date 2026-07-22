public class LifeCycleStatus : StatusEffect
{
    public LifeCycleStatus(int value)    
    {
        Name="Cycle de vie";
        Value = value;
        Duration = -1;
        inextendable=true;
        buff=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Quand vous perdez des PV, gagnez {Value} énergie.";
    }
    public override void OnHPLoss(Character target, int damage)
    {
        if (damage>0&&target.isPlayer)
        {
            if (target.onTurn)
            {
                target.GainEnergy(Value);
            }
            else
            {
                target.AddStatus(new EnergyUpStatus(Value,1));
            }
        }
    }
}