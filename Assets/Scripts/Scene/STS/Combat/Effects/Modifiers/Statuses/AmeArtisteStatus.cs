/// <summary>
/// Le vol d'un effet positif ne passe pas par un statut côté client : <c>EffectResolver</c> le
/// traite en un bloc, sans prévenir chaque statut du voleur. La description est donc tout ce que
/// ce statut a à montrer ; l'armure qu'il promet est accordée par le serveur.
/// </summary>
public class AmeArtisteStatus : StatusEffect
{
    public AmeArtisteStatus(int value)
    {
        Value = value;
        Duration = -1;
        Name = "Âme d'artiste";
        buff = true;
        framed = true;
    }
    public override string Desc(bool isPlayer)
    {
        return $"Quand vous volez un effet positif, gagnez {Value} d'Armure.";
    }
}
