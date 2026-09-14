using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public readonly struct AuthoritativeStatusState
{
    public string StatusType { get; }
    public int Value { get; }
    public int Duration { get; }
    public string CardId { get; }
    public int Index { get; }
    public int Progress { get; }

    /// <summary>
    /// Ce que le serveur dit de cette instance-là, par exception à son espèce : false quand
    /// aucune carte ordinaire ne peut la retirer (encadrée), null quand l'espèce décide.
    /// </summary>
    public bool? Removable { get; }

    /// <summary>true quand rien ne peut la retirer (cadre d'or), null quand l'espèce décide.</summary>
    public bool? ResistsEverything { get; }

    public AuthoritativeStatusState(string statusType, int value, int duration, string cardId, int index, int progress,
        bool? removable = null, bool? resistsEverything = null)
    {
        StatusType = statusType;
        Value = value;
        Duration = duration;
        CardId = cardId;
        Index = index;
        Progress = progress;
        Removable = removable;
        ResistsEverything = resistsEverything;
    }
}

public readonly struct AuthoritativeDamageState
{
    public int Hp { get; }
    public int Armor { get; }

    public AuthoritativeDamageState(int hp, int armor)
    {
        Hp = hp;
        Armor = armor;
    }
}

public static class AuthoritativeCombatStateReducer
{
    public static void MoveCard<T>(ICollection<T> source, ICollection<T> destination, T card)
    {
        if (source == null || destination == null)
            return;

        source.Remove(card);
        if (!destination.Contains(card))
            destination.Add(card);
    }

    public static AuthoritativeDamageState ResolveDamage(
        int currentHp,
        int currentArmor,
        int? remainingHp,
        int? remainingArmor)
    {
        return new AuthoritativeDamageState(
            remainingHp ?? currentHp,
            remainingArmor ?? currentArmor);
    }

    public static IReadOnlyList<AuthoritativeStatusState> ReadStatuses(JToken statusesToken)
    {
        var result = new List<AuthoritativeStatusState>();
        if (!(statusesToken is JArray statuses))
            return result;

        foreach (JToken token in statuses)
        {
            string statusType = token?.Value<string>("statusType");
            if (string.IsNullOrWhiteSpace(statusType))
                continue;

            result.Add(new AuthoritativeStatusState(
                statusType,
                token.Value<int?>("value") ?? 1,
                token.Value<int?>("duration") ?? -1,
                token.Value<string>("cardId") ?? string.Empty,
                token.Value<int?>("index") ?? 0,
                token.Value<int?>("progress") ?? 0,
                token.Value<bool?>("removable"),
                token.Value<bool?>("resistsEverything")));
        }
        return result;
    }

    public static int ResolveTurnCount(int currentTurnCount, string combatStatus)
    {
        if (currentTurnCount > 0)
            return currentTurnCount;

        return string.Equals(combatStatus, "ACTIVE", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(combatStatus, "FINISHED", System.StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
    }

    public static bool CanLoadEnteredNode(bool accepted, bool combatScene, bool hasEncounter)
    {
        return accepted && (!combatScene || hasEncounter);
    }
}
