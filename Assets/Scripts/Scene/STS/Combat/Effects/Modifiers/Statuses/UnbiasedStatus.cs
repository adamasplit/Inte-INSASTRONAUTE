public class UnbiasedStatus : StatusEffect
{
    public UnbiasedStatus(int duration)
    {
        Duration = duration;
        Name = "Impartial";
        buff = true;
        framed = true;
    }
    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Vos debuffs ignorent les résistances (Artefact, Filtre, etc.) de la cible pendant {turns}.";
        }
        return $"Les debuffs du personnage ignorent les résistances de la cible pendant {turns}.";
    }
}
