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
    /// <param name="owned">
    /// Le joueur a-t-il débloqué cette carte — par la collection pour celles qui y sont liées,
    /// par une fin de run pour les autres. Toute carte de campagne est verrouillée par défaut ;
    /// il n'y a plus de passe-droit pour celles sans lien de collection.
    /// </param>
    /// <param name="multiplayerExclusive">
    /// Non consulté ici : une exclusive multi se débloque désormais comme les autres, par fin de
    /// run, et non plus par niveau de personnage.
    /// </param>
    /// <param name="requiredCharacterLevel">Non consulté, voir <paramref name="multiplayerExclusive"/>.</param>
    /// <param name="characterLevel">Non consulté, voir <paramref name="multiplayerExclusive"/>.</param>
    public static bool IsUsable(
        string collectionCardId,
        bool owned,
        bool multiplayerExclusive,
        int requiredCharacterLevel,
        int characterLevel)
    {
        return owned;
    }
}

