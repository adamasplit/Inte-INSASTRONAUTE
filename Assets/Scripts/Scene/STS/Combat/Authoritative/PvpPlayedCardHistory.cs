using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// Une carte jouée, telle que l'historique partagé du duel la retient.
/// </summary>
public sealed class PvpPlayedCard
{
    public PvpPlayedCard(
        long revision,
        string actorId,
        string cardInstanceId,
        string definitionId,
        IReadOnlyList<string> targetIds)
    {
        Revision = revision;
        ActorId = actorId;
        CardInstanceId = cardInstanceId;
        DefinitionId = definitionId;
        TargetIds = targetIds ?? Array.Empty<string>();
    }

    public long Revision { get; }
    public string ActorId { get; }
    public string CardInstanceId { get; }
    public string DefinitionId { get; }
    public IReadOnlyList<string> TargetIds { get; }

    /// <summary>
    /// Ce qui identifie cette entrée d'un rafraîchissement à l'autre.
    ///
    /// <para>La révision seule ne suffit pas : une commande qui enchaîne plusieurs cartes — un
    /// renfort, un boss qui joue derrière le joueur — les marque toutes de la même révision
    /// finale. L'instance de carte les départage, et la révision garde l'ordre.</para>
    /// </summary>
    public string Key => Revision + "|" + CardInstanceId + "|" + DefinitionId;
}

/// <summary>
/// La lecture de l'historique partagé qu'un état PVP transporte.
///
/// <para>Il est <b>servi par le serveur</b> et non accumulé ici, et c'est ce qui fait qu'il est
/// partagé : une liste tenue par le client repartirait vide à la moindre reconnexion, puisqu'un
/// snapshot ne rejoue aucun événement — il ne porte qu'un état. Deux joueurs dont l'un s'est
/// reconnecté verraient alors deux historiques différents.</para>
///
/// <para>Sans dépendance à Unity, pour que « qu'est-ce que cet état dit avoir été joué ? » se
/// teste en C# pur.</para>
/// </summary>
public static class PvpPlayedCardHistory
{
    /// <summary>
    /// Les cartes que <paramref name="combatToken"/> dit avoir été jouées, dans l'ordre.
    ///
    /// <para>Un état sans historique — une partie PvE, un serveur plus ancien — en rend une liste
    /// vide plutôt que null : l'absence d'historique et un historique vide s'affichent pareil, et
    /// distinguer les deux n'apporterait qu'un test de nullité de plus à chaque appelant.</para>
    /// </summary>
    public static List<PvpPlayedCard> Read(JToken combatToken)
    {
        var played = new List<PvpPlayedCard>();
        if (!(combatToken is JObject combat) || !(combat["playedHistory"] is JArray history))
            return played;

        foreach (JToken entry in history)
        {
            if (!(entry is JObject card))
                continue;

            string definitionId = card.Value<string>("definitionId");
            string actorId = card.Value<string>("actorId");
            // Une entrée qui ne nomme aucune carte n'a rien à montrer : l'écarter vaut mieux que
            // d'ouvrir une vignette vide sur laquelle on peut cliquer.
            if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(actorId))
                continue;

            var targetIds = new List<string>();
            if (card["targetIds"] is JArray targets)
            {
                foreach (JToken target in targets)
                {
                    string value = target?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(value))
                        targetIds.Add(value);
                }
            }

            played.Add(new PvpPlayedCard(
                card.Value<long?>("revision") ?? 0L,
                actorId,
                card.Value<string>("cardInstanceId"),
                definitionId,
                targetIds));
        }

        return played;
    }

    /// <summary>
    /// Vrai quand <paramref name="next"/> dit autre chose que <paramref name="current"/>.
    ///
    /// <para>L'état complet arrive à chaque coup joué, et reconstruire la liste à chaque fois
    /// détruirait les vignettes sous le doigt du joueur — y compris celle qu'il vient d'ouvrir en
    /// grand. On ne rebâtit donc que quand le contenu a réellement changé.</para>
    /// </summary>
    public static bool Differs(List<PvpPlayedCard> current, List<PvpPlayedCard> next)
    {
        if (current == null || next == null)
            return current != next;
        if (current.Count != next.Count)
            return true;

        for (int index = 0; index < current.Count; index++)
        {
            if (!string.Equals(current[index].Key, next[index].Key, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
