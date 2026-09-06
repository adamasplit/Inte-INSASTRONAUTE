public class FortifyStatus : StatusEffect
{
    public FortifyStatus()
    {
        Duration = -1;
        Name = "Fortification";
        buff = true;
        generic = true;
    }

    public override string Desc(bool isPlayer)
    {
        return "Empêche la prochaine destruction d'Armure.";
    }
}
