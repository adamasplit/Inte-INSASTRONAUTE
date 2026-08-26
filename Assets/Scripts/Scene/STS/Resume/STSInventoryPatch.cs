using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// Ce qu'un patch d'inventaire du serveur contient, lu une bonne fois.
///
/// <para>Le serveur renvoie ces patchs après un choix d'événement ou une fin de nœud,
/// et le client se contentait de les ranger sans jamais les ouvrir : l'or et les cartes
/// gagnés n'apparaissaient qu'au prochain resynchro complet. Le gain existait, il était
/// simplement invisible — et rien ne le signalait.</para>
///
/// <para>La lecture vit ici, dans l'assembly de logique pure, pour être testable : les
/// tests EditMode ne voient pas Assembly-CSharp. L'application, elle, reste dans
/// RunManager, qui seul connaît le deck et les reliques.</para>
///
/// <para>Toutes les réponses ne portent pas toutes les clés. Une clé absente vaut
/// « rien à faire » plutôt qu'une erreur, sinon un patch partiel ferait échouer
/// l'application de ce qu'il contient bel et bien.</para>
/// </summary>
public class STSInventoryPatch
{
    public int GoldDelta { get; private set; }
    public List<string> RemovedCardInstanceIds { get; } = new();
    public List<JToken> AddedCards { get; } = new();
    public List<JToken> EnchantedCards { get; } = new();
    public List<JToken> AddedRelics { get; } = new();
    public List<JToken> UpdatedRelics { get; } = new();

    public static STSInventoryPatch Read(JToken patch)
    {
        var read = new STSInventoryPatch();
        if (patch == null || patch.Type != JTokenType.Object)
            return read;

        read.GoldDelta = patch["goldDelta"]?.Value<int>() ?? 0;

        foreach (JToken id in AsArray(patch["removedCardInstanceIds"]))
        {
            string value = id?.Type == JTokenType.String ? id.Value<string>() : null;
            if (!string.IsNullOrWhiteSpace(value))
                read.RemovedCardInstanceIds.Add(value);
        }

        CollectObjects(patch["addedCards"], read.AddedCards);
        CollectObjects(patch["enchantedCards"], read.EnchantedCards);
        CollectObjects(patch["addedRelics"], read.AddedRelics);
        CollectObjects(patch["updatedRelics"], read.UpdatedRelics);

        return read;
    }

    static IEnumerable<JToken> AsArray(JToken token)
    {
        return token != null && token.Type == JTokenType.Array ? token : new JArray();
    }

    static void CollectObjects(JToken token, List<JToken> into)
    {
        foreach (JToken item in AsArray(token))
        {
            if (item != null && item.Type == JTokenType.Object)
                into.Add(item);
        }
    }
}
