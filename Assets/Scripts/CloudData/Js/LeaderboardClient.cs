using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

[System.Serializable]
public class LeaderboardEntry
{
    public int rank;
    public string playerId;
    public string displayName;
    public long score;
}

[System.Serializable]
public class LeaderboardResult
{
    public bool ok;
    public List<LeaderboardEntry> topPlayers;
    public LeaderboardEntry currentPlayer;
    public string message;
}

[System.Serializable]
public class MetadataWrapper
{
    public string displayName;
}

public static class LeaderboardClient
{
    private const string LeaderboardId = "PC";

    /// <summary>
    /// Extrait le displayName depuis le metadata JSON
    /// </summary>
    private static string ParseDisplayName(string metadata, string fallback = "Player")
    {
        if (string.IsNullOrEmpty(metadata))
            return fallback;

        // Essayer JsonUtility d'abord
        try
        {
            var meta = JsonUtility.FromJson<MetadataWrapper>(metadata);
            if (meta != null && !string.IsNullOrEmpty(meta.displayName))
            {
                return meta.displayName;
            }
        }
        catch { }

        // Fallback: parsing manuel avec regex
        try
        {
            var match = Regex.Match(metadata, @"""displayName""\s*:\s*""([^""]+)""");
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }
        catch { }

        return fallback;
    }

    /// <summary>
    /// Soumet le score PC du joueur courant au leaderboard
    /// </summary>
    public static async Task<bool> SubmitScoreAsync(long score)
    {
        try
        {
            Debug.Log($"[LeaderboardClient] Submitting score {score} to leaderboard {LeaderboardId}");
            
            // Metadata doit être un Dictionary<string, object>
            var options = new AddPlayerScoreOptions
            {
                Metadata = new Dictionary<string, object>
                {
                    { "displayName", PlayerProfileStore.DISPLAY_NAME }
                }
            };
            
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score, options);
            Debug.Log("[LeaderboardClient] Score submitted successfully");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LeaderboardClient] Submit failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Récupère le top 10 + le rang du joueur courant si pas dans le top 10
    /// </summary>
    public static async Task<LeaderboardResult> GetLeaderboardAsync()
    {
        var result = new LeaderboardResult
        {
            ok = false,
            topPlayers = new List<LeaderboardEntry>(),
            currentPlayer = null,
            message = ""
        };

        try
        {
            Debug.Log("[LeaderboardClient] Fetching leaderboard...");

            // 1. Récupérer le top 10 avec metadata
            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(
                LeaderboardId,
                new GetScoresOptions 
                { 
                    Offset = 0, 
                    Limit = 10,
                    IncludeMetadata = true  // Important: inclure le metadata!
                }
            );

            var topPlayerIds = new HashSet<string>();

            foreach (var entry in scoresResponse.Results)
            {
                topPlayerIds.Add(entry.PlayerId);
                
                Debug.Log($"[LeaderboardClient] Raw metadata for {entry.PlayerId}: '{entry.Metadata}'");
                string displayName = ParseDisplayName(entry.Metadata, "Player");
                Debug.Log($"[LeaderboardClient] Parsed displayName: '{displayName}'");

                result.topPlayers.Add(new LeaderboardEntry
                {
                    rank = entry.Rank + 1,
                    playerId = entry.PlayerId,
                    displayName = displayName,
                    score = (long)entry.Score
                });
            }

            // 2. Récupérer le score du joueur courant
            try
            {
                var playerScore = await LeaderboardsService.Instance.GetPlayerScoreAsync(
                    LeaderboardId,
                    new GetPlayerScoreOptions { IncludeMetadata = true }
                );
                
                // Si le joueur n'est pas dans le top 10, l'ajouter
                if (!topPlayerIds.Contains(playerScore.PlayerId))
                {
                    string displayName = ParseDisplayName(playerScore.Metadata, PlayerProfileStore.DISPLAY_NAME);

                    result.currentPlayer = new LeaderboardEntry
                    {
                        rank = playerScore.Rank + 1,
                        playerId = playerScore.PlayerId,
                        displayName = displayName,
                        score = (long)playerScore.Score
                    };
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"[LeaderboardClient] Player has no score yet: {e.Message}");
            }

            result.ok = true;
            result.message = "OK";
            Debug.Log($"[LeaderboardClient] Got {result.topPlayers.Count} entries");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LeaderboardClient] GetLeaderboard failed: {e.Message}");
            result.message = e.Message;
        }

        return result;
    }
}
