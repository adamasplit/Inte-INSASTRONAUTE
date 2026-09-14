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
        return "Quand vous volez, transférez ou dissipez un effet encadré avec une carte, gagnez 1 de Force.";
    }
}
