public class InnocenceStatus : StatusEffect
{
    public InnocenceStatus()
    {
        Name = "Innocence";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer) => "Si vous n'infligez pas de dégâts à votre tour, gagnez 60% de réduction de dégâts jusqu'au prochain.";
}