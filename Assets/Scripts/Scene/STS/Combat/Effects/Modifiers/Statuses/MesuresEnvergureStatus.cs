public class MesuresEnvergureStatus : StatusEffect
{
    public MesuresEnvergureStatus()
    {
        Name = "Mesures d'envergure";
        Duration = -1;
        buff = true;
    }

    public override string Desc(bool isPlayer) => "Quand vous ciblez plus d'un personnage avec une attaque, infligez 25% de dégâts en plus, y compris à vous-même.";
}