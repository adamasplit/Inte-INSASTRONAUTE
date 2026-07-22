public class StunStatus : StatusEffect
{
    public StunStatus(int duration)
    {
        Value = 0;
        statusType = StatusType.Stun;
        Duration = duration;
        Name = "Étourdissement";
        framed=true;
        debuff=true;
        goldFrame=true;
        generic=true;
        inextendable=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Un personnage étourdi ne peut pas agir.";
    }
}