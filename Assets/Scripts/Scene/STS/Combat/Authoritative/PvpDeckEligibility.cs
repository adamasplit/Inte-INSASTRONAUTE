using System;

/// <summary>
/// Ce qu'un joueur a le droit de mettre dans un deck multijoueur.
/// </summary>
/// <remarks>
/// <para>La règle appartient au serveur, qui refuse la sauvegarde d'un deck :
/// seule une carte liée à une carte de collection demande de la posséder. Les
/// autres — plus de trois cents sur trois cent cinquante — sont libres.</para>
///
/// <para>Le constructeur de deck exigeait de tout posséder, y compris ce que le
/// serveur donne à tout le monde. Un joueur sans collection voyait donc une grille
/// vide et ne pouvait composer aucun deck, alors que l'API en aurait accepté un.
/// Il ne pouvait pas non plus chercher de combat, faute de deck : le multijoueur
/// était fermé à qui n'avait jamais scanné une carte.</para>
///
/// <para>Cette règle est ici, en C# pur, pour être vérifiée sans ouvrir Unity et
/// pour qu'un écart avec le serveur se voie au lieu de se deviner.</para>
/// </remarks>
public static class PvpDeckEligibility
{
    /// <summary>
    /// La carte appartient-elle à la réserve d'un personnage donné.
    /// </summary>
    /// <remarks>
    /// Cette règle était écrite trois fois dans le constructeur de deck, et deux de
    /// ces copies oubliaient les cartes de départ : quarante et une cartes que le
    /// code déclarait par ailleurs jouables n'apparaissaient jamais dans la grille.
    /// Pour EP, la réserve tombait de soixante-dix-sept cartes à trente-six.
    /// </remarks>
    public static bool BelongsToPool(string favoredCharacter, string selectedCharacter)
    {
        if (string.IsNullOrWhiteSpace(favoredCharacter))
        {
            return true;
        }

        return favoredCharacter.Equals("Aucun", StringComparison.OrdinalIgnoreCase)
            || favoredCharacter.Equals(selectedCharacter, StringComparison.OrdinalIgnoreCase);
    }

    /// <param name="collectionCardId">
    /// L'identifiant de collection de la carte, vide quand elle n'en a pas.
    /// </param>
    /// <param name="owned">Le joueur possède-t-il cette carte de collection.</param>
    /// <param name="multiplayerExclusive">La carte est-elle réservée au multijoueur.</param>
    /// <param name="requiredCharacterLevel">Le niveau qu'elle exige, le cas échéant.</param>
    /// <param name="characterLevel">Le niveau atteint par le joueur.</param>
    public static bool IsUsable(
        string collectionCardId,
        bool owned,
        bool multiplayerExclusive,
        int requiredCharacterLevel,
        int characterLevel)
    {
        // Une exclusive multi ne s'achète pas, elle se débloque.
        if (multiplayerExclusive)
        {
            return characterLevel >= requiredCharacterLevel;
        }

        // Pas d'identifiant de collection : la carte appartient au jeu de base.
        if (string.IsNullOrWhiteSpace(collectionCardId))
        {
            return true;
        }

        return owned;
    }
}
