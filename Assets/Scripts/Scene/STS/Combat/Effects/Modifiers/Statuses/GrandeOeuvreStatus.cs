/// <summary>La Vigueur qu'elle promet est accordée par <c>EffectResolver.TransferDebuff</c>, pas par ce statut lui-même.</summary>
public class GrandeOeuvreStatus : StatusEffect
{
    public GrandeOeuvreStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Grande œuvre";
        buff = true;
        framed = true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Quand vous transférez un effet, gagnez {Value} de Vigueur.";
    }
}
