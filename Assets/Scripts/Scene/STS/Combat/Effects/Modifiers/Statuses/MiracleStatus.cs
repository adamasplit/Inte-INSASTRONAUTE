public class MiracleStatus : StatusEffect
{
    public MiracleStatus(int value)
    {
        Name = "Miracle";
        Value = value;
        Duration = -1;
        buff = true;
        framed = true;
        generic=true;
    }

    public override int ValidateHPLoss(int damage, Character target)
    {
        if (Value <= 0 || damage < target.currentHP)
            return damage;

        Value--;
        if (Value == 0)
            mustExpire = true;
        return target.currentHP - 1;
    }

    public override string Desc(bool isPlayer)
    {
        return (isPlayer ? "Vos PV" : "Les PV du personnage")
            + " sont remis à 1 s'ils devaient tomber à 0. S'active " + Value
            + " fois.";
    }
}