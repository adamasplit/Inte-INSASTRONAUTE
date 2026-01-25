using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;

[Serializable]
public class PlaceBetResponse
{
    public bool ok;
    public string message;
    public BetDto bet;
    public string eventId;
}

[Serializable]
public class BetDto
{
    public string eventId;
    public int amount;
    public float odds;
    public bool side;          // true = YES, false = NO
    public string placedIso;
    public bool resolved;
}

[Serializable]
public class ResolveBetsResponse
{
    public bool ok;
    public ResolvedBet[] resolved;
    public string message;
}

[Serializable]
public class ResolvedBet
{
    public string eventId;
    public bool win;           // true if player won, false if lost
    public int refund;         // TOKEN amount credited (already done by Cloud Code)
}

public static class BetsClient
{
    // Appelé quand l'utilisateur confirme un pari
    public static Task<PlaceBetResponse> PlaceBetAsync(EventDto e, int amount, bool sideYes)
    {
        var args = new Dictionary<string, object>
        {
            { "eventId", e.id },
            { "amount", amount },
            { "side", sideYes },
            { "odds", e.odds },
            { "deadlineIso", e.deadlineIso },
            { "status", e.status }
        };

        return CloudCodeService.Instance.CallEndpointAsync<PlaceBetResponse>(
            "PlaceBet",
            args
        );
    }

    // Appelé à la connexion / refresh pour résoudre les paris clôturés
    public static async Task<ResolveBetsResponse> ResolveBetsAsync(EventDto[] allEvents)
    {
        var list = new List<Dictionary<string, object>>();

        foreach (var e in allEvents)
        {
            // Only send PARI events that are CLOSED and have an outcome
            if (e.type == "PARI" && e.status == "CLOSED" && e.HasOutcome)
            {
                list.Add(new Dictionary<string, object>
                {
                    { "id", e.id },
                    { "status", e.status },
                    { "outcome", e.OutcomeValue }
                });
            }
        }

        var args = new Dictionary<string, object>
        {
            { "events", list }
        };

        try
        {
            var response = await CloudCodeService.Instance.CallEndpointAsync<ResolveBetsResponse>(
                "ResolveBets",
                args
            );
            return response;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[BetsClient] ResolveBets error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }
}
