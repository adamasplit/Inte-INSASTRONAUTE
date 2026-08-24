using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

/// <summary>
/// Ce qu'une notification de matchmaking nous apprend : quelle bataille, et quelle
/// notification acquitter ensuite.
/// </summary>
public readonly struct PvpMatchNotification
{
    public PvpMatchNotification(string notificationId, string battleId)
    {
        NotificationId = notificationId;
        BattleId = battleId;
    }

    /// L'identifiant à envoyer à l'accusé de réception. Peut être vide : une bataille
    /// nommée sans notification identifiable reste une bataille où entrer.
    public string NotificationId { get; }

    public string BattleId { get; }

    public bool Found => !string.IsNullOrWhiteSpace(BattleId);

    public static PvpMatchNotification None => default;
}

/// <summary>
/// La lecture d'une liste de notifications PVP.
///
/// <para>Le joueur qui s'inscrit le premier ne reçoit pas de <c>battleId</c> en réponse à
/// son inscription : c'est le second, celui dont la demande referme l'appariement, qui en
/// obtient un. Le serveur crée alors une notification <c>QUICK_MATCH_FOUND</c> <b>pour les
/// deux</b>, et c'est le seul moyen qu'a le premier d'apprendre que quelqu'un est
/// arrivé.</para>
///
/// <para>Une notification a la forme <c>{ id, type, title, body, actorUserId, read,
/// createdAt, payload }</c>, et celle qui nous intéresse porte
/// <c>payload.battleId</c>. Les autres types — <c>CHALLENGE_RECEIVED</c>,
/// <c>CHALLENGE_ACCEPTED</c>, <c>CHALLENGE_DECLINED</c>, <c>BATTLE_UPDATED</c>,
/// <c>INFO</c> — peuvent nommer une bataille eux aussi : <b>lire le premier
/// <c>battleId</c> venu ferait entrer dans une bataille terminée</b>. Le type est donc
/// filtré, pas deviné.</para>
///
/// <para>Une notification déjà lue est une notification déjà agie : elle est ignorée, de
/// sorte qu'un acquittement passé suffise à ne plus jamais y revenir.</para>
/// </summary>
public static class PvpMatchNotifications
{
    /// Le seul type qui annonce un appariement. Comparé sans tenir compte de la casse :
    /// aucun des six types du serveur ne se distingue d'un autre par elle, donc tolérer la
    /// casse ne peut pas confondre deux types — alors qu'un type manqué rendrait le mode
    /// injouable pour le joueur qui a attendu.
    public const string QuickMatchFoundType = "QUICK_MATCH_FOUND";

    /// <summary>
    /// L'appariement le plus récent qu'on n'a pas encore acquitté, s'il y en a un.
    /// </summary>
    public static PvpMatchNotification FindQuickMatch(JToken notifications)
    {
        JArray list = ReadList(notifications);
        if (list == null)
            return PvpMatchNotification.None;

        PvpMatchNotification best = PvpMatchNotification.None;
        DateTimeOffset bestCreatedAt = default;
        bool bestHasCreatedAt = false;

        foreach (JToken token in list)
        {
            if (!(token is JObject notification) || !IsUnreadQuickMatch(notification))
                continue;

            string battleId = ReadBattleId(notification);
            if (string.IsNullOrWhiteSpace(battleId))
                continue;

            bool hasCreatedAt = TryReadCreatedAt(notification, out DateTimeOffset createdAt);

            if (best.Found && (!hasCreatedAt || (bestHasCreatedAt && createdAt <= bestCreatedAt)))
                continue;

            best = new PvpMatchNotification(ReadId(notification), battleId);
            bestCreatedAt = createdAt;
            bestHasCreatedAt = hasCreatedAt;
        }

        return best;
    }

    /// <summary>
    /// Toutes les notifications d'appariement non lues qui désignent cette bataille.
    ///
    /// <para>Le joueur qui a reçu son <c>battleId</c> directement en entre sans jamais
    /// passer par la liste : sa notification resterait non lue, et le ferait entrer dans
    /// ce combat terminé la prochaine fois qu'il cherche un adversaire. Les deux joueurs
    /// acquittent donc par bataille, et pas seulement celui qui a vu la
    /// notification.</para>
    /// </summary>
    public static IReadOnlyList<string> QuickMatchIdsForBattle(JToken notifications, string battleId)
    {
        var ids = new List<string>();
        JArray list = ReadList(notifications);
        if (list == null || string.IsNullOrWhiteSpace(battleId))
            return ids;

        string wanted = battleId.Trim();

        foreach (JToken token in list)
        {
            if (!(token is JObject notification) || !IsUnreadQuickMatch(notification))
                continue;

            if (!string.Equals(ReadBattleId(notification), wanted, StringComparison.OrdinalIgnoreCase))
                continue;

            string id = ReadId(notification);
            if (!string.IsNullOrWhiteSpace(id))
                ids.Add(id);
        }

        return ids;
    }

    /// <summary>
    /// La liste elle-même, qu'elle arrive nue ou dans une enveloppe. L'enveloppe du pont
    /// React est déjà défaite en amont ; celles-ci sont les formes de pagination usuelles.
    /// </summary>
    private static JArray ReadList(JToken notifications)
    {
        if (notifications is JArray array)
            return array;

        if (notifications is JObject wrapper)
        {
            foreach (string key in new[] { "notifications", "items", "content" })
            {
                if (wrapper.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nested)
                    && nested is JArray nestedArray)
                {
                    return nestedArray;
                }
            }
        }

        return null;
    }

    private static bool IsUnreadQuickMatch(JObject notification)
    {
        string type = notification.Value<string>("type");
        if (!string.Equals(type?.Trim(), QuickMatchFoundType, StringComparison.OrdinalIgnoreCase))
            return false;

        return !(notification.Value<bool?>("read") ?? false);
    }

    private static string ReadBattleId(JObject notification)
    {
        string battleId = (notification["payload"] as JObject)?.Value<string>("battleId");
        return string.IsNullOrWhiteSpace(battleId) ? null : battleId.Trim();
    }

    private static string ReadId(JObject notification)
    {
        string id = notification.Value<string>("id");
        return string.IsNullOrWhiteSpace(id) ? null : id.Trim();
    }

    private static bool TryReadCreatedAt(JObject notification, out DateTimeOffset createdAt)
    {
        return DateTimeOffset.TryParse(
            notification.Value<string>("createdAt"),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out createdAt);
    }
}
