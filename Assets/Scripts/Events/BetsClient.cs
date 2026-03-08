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
    public string answerType;  // "list" | "free"
    public string choice;      // label choisi (list) ou réponse saisie (free)
    public float odds;         // côte au moment du pari (list) ; 0 pour free
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
    public bool win;
    public int refund;   // montant TOKEN déjà crédité par le Cloud Code
}

public static class BetsClient
{
    /// <summary>Enregistre un pari sur un événement.</summary>
    /// <param name="choice">Label de l'option (list) ou réponse libre (free).</param>
    /// <param name="odds">Côte de l'option choisie (list) ; passer 0 pour free.</param>
    public static Task<PlaceBetResponse> PlaceBetAsync(EventDto e, int amount, string choice, float odds)
    {
        var args = new Dictionary<string, object>
        {
            { "eventId",     e.id },
            { "amount",      amount },
            { "choice",      choice },
            { "odds",        odds },
            { "answerType",  e.answerType },
            { "deadlineIso", e.deadlineIso },
            { "status",      e.status }
        };

        return CloudCodeService.Instance.CallEndpointAsync<PlaceBetResponse>(
            "PlaceBet",
            args
        );
    }

    /// <summary>Résout les paris des événements clôturés. À appeler à la connexion/refresh.</summary>
    public static async Task<ResolveBetsResponse> ResolveBetsAsync(EventDto[] allEvents)
    {
        var list = new List<Dictionary<string, object>>();

        foreach (var e in allEvents)
        {
            if (e.type != "PARI" || e.status != "CLOSED" || !e.HasOutcome)
                continue;

            var entry = new Dictionary<string, object>
            {
                { "id",         e.id },
                { "status",     e.status },
                { "answerType", e.answerType }
            };

            if (e.answerType == "free")
            {
                // Envoie la liste des bonnes réponses avec leurs côtes
                var outcomesList = new List<Dictionary<string, object>>();
                if (e.outcomes != null)
                    foreach (var o in e.outcomes)
                        outcomesList.Add(new Dictionary<string, object>
                        {
                            { "answer", o.answer },
                            { "odds",   o.odds }
                        });
                entry["outcomes"] = outcomesList;
            }
            else
            {
                // Envoie le label du choix gagnant
                entry["outcome"] = e.outcome;
            }

            list.Add(entry);
        }

        var args = new Dictionary<string, object> { { "events", list } };

        try
        {
            return await CloudCodeService.Instance.CallEndpointAsync<ResolveBetsResponse>(
                "ResolveBets",
                args
            );
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[BetsClient] ResolveBets error: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }
}
