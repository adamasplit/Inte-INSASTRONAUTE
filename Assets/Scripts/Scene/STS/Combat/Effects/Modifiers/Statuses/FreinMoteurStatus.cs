public class FreinMoteurStatus : StatusEffect
{
    private int hpAtTurnStart;

    public FreinMoteurStatus()
    {
        Duration = -1;
        Name = "Frein moteur";
        buff = true;
        framed = true;
        generic = true;
    }
    public override void OnTurnStart(Character target)
    {
        hpAtTurnStart = target.currentHP;
    }
    public override void OnTurnEnd(Character target)
    {
        int lost = hpAtTurnStart - target.currentHP;
        if (lost > 0)
        {
            target.Heal(lost / 2);
        }
    }
    public override string Desc(bool isPlayer)
    {
        return "Quand vous perdez des PV pendant votre tour, regagnez-en la moitié à la fin du tour.";
    }
}
