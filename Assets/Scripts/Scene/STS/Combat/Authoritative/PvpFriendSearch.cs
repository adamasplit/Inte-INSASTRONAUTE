using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

public readonly struct PvpFriend
{
    public PvpFriend(string userId, string displayName)
    {
        UserId = userId?.Trim();
        DisplayName = displayName?.Trim();
    }

    public string UserId { get; }
    public string DisplayName { get; }

    public bool IsValid => !string.IsNullOrWhiteSpace(UserId)
        && !string.IsNullOrWhiteSpace(DisplayName);
}

public static class PvpFriendSearch
{
    public static IReadOnlyList<PvpFriend> Filter(
        IEnumerable<PvpFriend> friends,
        string query,
        int limit)
    {
        if (friends == null || string.IsNullOrWhiteSpace(query) || limit <= 0)
            return Array.Empty<PvpFriend>();

        string wanted = Normalize(query);
        return friends
            .Where(friend => friend.IsValid && Normalize(friend.DisplayName).Contains(wanted))
            .OrderBy(friend => Normalize(friend.DisplayName).StartsWith(wanted) ? 0 : 1)
            .ThenBy(friend => friend.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);
        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                result.Append(char.ToLowerInvariant(character));
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }
}
