public class MegacristalStatus : StatusEffect
{
    public MegacristalStatus()
    {
        Name = "Mégacristal";
        Duration = -1;
        debuff = true;
        framed = true;
        goldFrame = true;
    }

    public override string Desc(bool isPlayer) => "Cristallisation devient indissipable et demande 1 coup de moins pour s'activer.";
}