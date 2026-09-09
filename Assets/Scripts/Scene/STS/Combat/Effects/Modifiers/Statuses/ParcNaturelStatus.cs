/// <summary>Voir <see cref="AmeArtisteStatus"/> : partager le vol avec les alliés se joue côté serveur.</summary>
public class ParcNaturelStatus : StatusEffect
{
    public ParcNaturelStatus()
    {
        Duration = -1;
        Name = "Parc naturel";
        buff = true;
        framed = true;
        generic = true;
    }
    public override string Desc(bool isPlayer)
    {
        return "Quand vous volez un effet positif, appliquez-le aussi à tous vos alliés.";
    }
}
