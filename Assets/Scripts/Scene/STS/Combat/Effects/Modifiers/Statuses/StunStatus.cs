public class StunStatus : StatusEffect
{
    public StunStatus(int duration)
    {
        Value = 0;
        Duration = duration;
        Name = "Étourdissement";
        framed=true;
        generic=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Ce personnage est étourdi et ne peut pas agir.";
    }
    public override void OnTurnEnd(Character character)
    {
        mustExpire=true;
    }
}