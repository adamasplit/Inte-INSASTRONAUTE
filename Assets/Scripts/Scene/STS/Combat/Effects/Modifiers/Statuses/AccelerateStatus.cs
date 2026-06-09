public class AccelerateStatus : StatusEffect
{
    public AccelerateStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Accélération";
        modifierType = ModifierType.Additive;
        buff=true;
        framed=true;
    }
    public override string Desc()
    {
        return $"Donne {Value} de Vitesse à chaque fin de tour";
    }
    public override void OnTurnEnd(Character character)
    {
        character.AddStatus(new SpeedStatus(Value));
    }
}