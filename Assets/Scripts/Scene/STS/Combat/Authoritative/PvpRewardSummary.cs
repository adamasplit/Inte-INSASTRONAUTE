using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

/// <summary>
/// Ce qu'un duel réglé a valu au joueur, mis en une phrase.
///
/// <para>Les montants viennent du serveur et ne se recalculent pas ici : ce qu'une victoire
/// rapporte dépend des classements <em>d'avant</em> le duel, et ceux-là ont déjà bougé au moment
/// où cet écran s'affiche. Le client ne fait donc que lire ce que la bataille a gardé.</para>
///
/// <para>Sans dépendance à Unity : « qu'est-ce que ce duel m'a rapporté ? » se teste en C# pur.</para>
/// </summary>
public static class PvpRewardSummary
{
    /// <summary>
    /// La phrase à montrer, ou null quand il n'y a rien à annoncer.
    /// </summary>
    /// <param name="battle">Le <c>StsPvpBattleDto</c> du duel réglé.</param>
    /// <param name="localUserId">
    /// Le joueur qui regarde. Un duel en oppose plusieurs et chacun a gagné autre chose ; sans
    /// savoir qui demande, il n'y a pas de bonne réponse — d'où null plutôt qu'une au hasard.
    /// </param>
    public static string Describe(JToken battle, string localUserId)
    {
        JToken reward = RewardFor(battle, localUserId);
        if (reward == null)
            return null;

        int eloDelta = reward.Value<int?>("eloDelta") ?? 0;
        long tokenDelta = reward.Value<long?>("tokenDelta") ?? 0L;
        int eloAfter = reward.Value<int?>("eloAfter") ?? 0;

        // Un duel amical ne rapporte rien et ne coûte rien : le dire serait annoncer du vide.
        if (eloDelta == 0 && tokenDelta == 0L)
            return null;

        string elo = $"{Signed(eloDelta)} classement (désormais {eloAfter})";
        if (tokenDelta == 0L)
            return elo + ".";

        return $"{elo}, {Signed(tokenDelta)} jetons.";
    }

    /// <summary>
    /// Ce que la bataille a gardé pour ce joueur, ou null.
    ///
    /// <para>Les gains sont rangés par identifiant de joueur, et la comparaison ignore la casse :
    /// un UUID reste le même identifiant qu'il soit écrit en majuscules ou non, et les deux bouts
    /// ne le formatent pas forcément pareil.</para>
    /// </summary>
    public static JToken RewardFor(JToken battle, string localUserId)
    {
        if (battle == null || string.IsNullOrWhiteSpace(localUserId))
            return null;
        if (!(battle["rewards"] is JObject rewards))
            return null;

        foreach (JProperty entry in rewards.Properties())
        {
            if (string.Equals(entry.Name, localUserId.Trim(), StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }
        return null;
    }

    /// « +16 » ou « -12 » : le signe est ce que le joueur lit en premier.
    private static string Signed(long value)
    {
        return value > 0
            ? "+" + value.ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }
}
