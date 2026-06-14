public class EnergyStatus : StatusEffect
{
    public EnergyStatus(int value)    
    {
        Name="Énergie";
        Value = value;
        Duration = -1;
        framed=true;
        buff=true;
    }
    public override string Desc()
    {
        return $"Gagnez {Value} énergie au début du tour.";
    }
    public override void OnTurnStart(Character target)
    {
        base.OnTurnStart(target);
        if (target.isPlayer)
        {
            target.GainEnergy(Value);
        }
    }
}