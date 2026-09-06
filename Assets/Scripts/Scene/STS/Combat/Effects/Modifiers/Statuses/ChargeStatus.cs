public class ChargeStatus : StatusEffect
{
    public ChargeStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Charge";
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        return $"Gagner de l'énergie accumule de la Charge. À 3, gagnez 2 de Vigueur puis recommencez à 0.";
    }
}
