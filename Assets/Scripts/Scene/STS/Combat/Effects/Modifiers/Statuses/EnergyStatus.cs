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
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Gagnez {Value} énergie au début du tour.";
        }
        return $"L'ennemi gagne {Value} énergie au début du tour.";
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