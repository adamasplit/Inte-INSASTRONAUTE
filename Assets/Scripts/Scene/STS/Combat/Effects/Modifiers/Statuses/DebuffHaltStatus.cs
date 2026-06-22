public class DebuffHaltStatus:StatusEffect
{
    public DebuffHaltStatus(int duration)
    {
        Duration = duration;
        Name = "Isolement thermique";
        modifierType = ModifierType.Additive;
        debuff=true;
        framed=true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"La durée des effets de debuff ne diminue plus pendant {Duration} tour"+(Duration>1?"s":"");
    }
}