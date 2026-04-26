using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

[System.Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string playerId;
    public string displayName;
    public long score;
    public string department;
    public string scoreTheme;
    public bool isFriend;
    public bool isCurrentPlayer;
}

[System.Serializable]
public class LeaderboardResult
{
    public bool ok;
    public string leaderboardId;
    public List<LeaderboardEntry> entries;
    public LeaderboardEntry currentPlayer;
    public List<LeaderboardEntry> friends;
    public int nextOffset;
    public bool hasMore;
    public string message;
}

[System.Serializable]
public class SubmitLeaderboardResult
{
    public bool ok;
    public string leaderboardId;
    public long score;
    public string message;
}

public static class LeaderboardClient
{
    public const string DefaultLeaderboardId = "PTD";

    public static string NormalizeLeaderboardId(string leaderboardId)
    {
        var normalized = (leaderboardId ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? DefaultLeaderboardId : normalized;
    }

    public static async Task<SubmitLeaderboardResult> SubmitScoreAsync(string leaderboardId, long score, string scoreTheme = "")
    {
        try
        {
            var requestLeaderboardId = NormalizeLeaderboardId(leaderboardId);
            Debug.Log($"[LeaderboardClient] Submitting score {score} to leaderboard {requestLeaderboardId}");

            var result = await CloudCodeService.Instance.CallEndpointAsync<SubmitLeaderboardResult>(
                "SubmitLeaderboardScore",
                new Dictionary<string, object>
                {
                    { "leaderboardId", requestLeaderboardId },
                    { "score", score },
                    { "scoreTheme", scoreTheme ?? string.Empty },
                    { "department", PlayerProfileStore.DEPARTMENT ?? string.Empty },
                    { "displayName", PlayerProfileStore.DISPLAY_NAME ?? string.Empty }
                }
            );

            return result ?? new SubmitLeaderboardResult
            {
                ok = false,
                leaderboardId = requestLeaderboardId,
                score = 0,
                message = "No response from Cloud Code"
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LeaderboardClient] Submit failed: {e.Message}");
            return new SubmitLeaderboardResult
            {
                ok = false,
                leaderboardId = NormalizeLeaderboardId(leaderboardId),
                score = 0,
                message = e.Message
            };
        }
    }

    public static async Task<LeaderboardResult> GetLeaderboardPageAsync(
        string leaderboardId,
        int offset,
        int limit,
        bool friendsOnly,
        string department,
        string theme,
        bool includeCurrentPlayer = true,
        bool includeFriends = true)
    {
        var requestLeaderboardId = NormalizeLeaderboardId(leaderboardId);

        var result = new LeaderboardResult
        {
            ok = false,
            leaderboardId = requestLeaderboardId,
            entries = new List<LeaderboardEntry>(),
            currentPlayer = null,
            friends = new List<LeaderboardEntry>(),
            nextOffset = offset,
            hasMore = false,
            message = ""
        };

        try
        {
            Debug.Log($"[LeaderboardClient] Fetching leaderboard {requestLeaderboardId} page offset={offset} limit={limit}");

            var response = await CloudCodeService.Instance.CallEndpointAsync<LeaderboardResult>(
                "GetLeaderboard",
                new Dictionary<string, object>
                {
                    { "leaderboardId", requestLeaderboardId },
                    { "offset", Mathf.Max(0, offset) },
                    { "limit", Mathf.Clamp(limit, 1, 50) },
                    { "friendsOnly", friendsOnly },
                    { "department", department ?? string.Empty },
                    { "theme", theme ?? string.Empty },
                    { "includeCurrentPlayer", includeCurrentPlayer },
                    { "includeFriends", includeFriends }
                }
            );

            if (response != null)
            {
                if (response.entries == null) response.entries = new List<LeaderboardEntry>();
                if (response.friends == null) response.friends = new List<LeaderboardEntry>();
                return response;
            }

            result.message = "No response from Cloud Code";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LeaderboardClient] GetLeaderboard failed: {e.Message}");
            result.message = e.Message;
        }

        return result;
    }
}
