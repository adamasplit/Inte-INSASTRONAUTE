public class PrecisionStatus : StatusEffect
{
    public PrecisionStatus()
    {
        Duration = -1;
        Name = "Précision";
        buff = true;
        generic = true;
    }

    public override string Desc(bool isPlayer)
    {
        return "Votre prochaine Attaque à cible unique ne peut pas infliger moins que ses dégâts de base. Se consomme seulement si elle augmente les dégâts.";
    }
}
