/// <summary>Voir <see cref="AmeArtisteStatus"/> : la Force gagnée est accordée côté serveur.</summary>
public class PavillonStatus : StatusEffect
{
    public PavillonStatus()
    {
        Duration = -1;
        Name = "Pavillon";
        buff = true;
        framed = true;
        generic = true;
    }
    public override string Desc(bool isPlayer)
    {
        return "Quand vous volez ou transférez un effet encadré, gagnez 1 de Force.";
    }
}
