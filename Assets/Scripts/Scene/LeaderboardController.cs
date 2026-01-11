using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;

public class LeaderboardController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject leaderboardElementPrefab;
    public Transform contentParent;
    
    [Header("Current Player Section (optional)")]
    [Tooltip("Séparateur affiché avant le rang du joueur courant si hors top 10")]
    public GameObject separatorPrefab;
    
    [Header("Settings")]
    [SerializeField] private bool submitScoreOnStart = true;
    [SerializeField] private bool useDummyData = false;

    private string currentPlayerId;

    void Start()
    {
        if (useDummyData)
        {
            Debug.Log("Populating leaderboard with dummy data.");
            DummyPopulate();
            return;
        }

        currentPlayerId = AuthenticationService.Instance.PlayerId;
        
        
    }

    /// <summary>
    /// Rafraîchit le leaderboard : soumet le score puis récupère les données
    /// </summary>
    public async Task RefreshLeaderboardAsync()
    {
        ClearLeaderboard();

        try
        {
            // 1. Soumettre le score PC du joueur (depuis Economy)
            if (submitScoreOnStart)
            {
                Debug.Log($"[Leaderboard] Submitting score with displayName: {PlayerProfileStore.DISPLAY_NAME}");
                
                // Récupérer le score PC depuis PlayerProfileStore
                bool submitted = await LeaderboardClient.SubmitScoreAsync(PlayerProfileStore.PC);
                if (!submitted)
                {
                    Debug.LogWarning("[Leaderboard] Submit failed");
                }
            }

            // 2. Récupérer le leaderboard
            Debug.Log("[Leaderboard] Fetching leaderboard...");
            var result = await LeaderboardClient.GetLeaderboardAsync();

            if (!result.ok)
            {
                Debug.LogError($"[Leaderboard] Failed to get leaderboard: {result.message}");
                return;
            }

            // 3. Afficher le top 10
            bool currentPlayerInTop = false;
            foreach (var entry in result.topPlayers)
            {
                Color? bgColor = GetRankColor(entry.rank);
                bool isCurrentPlayer = entry.playerId == currentPlayerId;
                
                if (isCurrentPlayer)
                {
                    currentPlayerInTop = true;
                    bgColor = new Color(0.2f, 0.6f, 1f, 1f); // Bleu pour le joueur courant
                }

                AddLeaderboardEntry(entry.rank, entry.displayName, (int)entry.score, null, bgColor);
            }

            // 4. Si le joueur courant n'est pas dans le top 10, l'afficher en bas
            if (!currentPlayerInTop && result.currentPlayer != null)
            {
                // Ajouter un séparateur visuel
                if (separatorPrefab != null)
                {
                    Instantiate(separatorPrefab, contentParent);
                }
                else
                {
                    // Créer un séparateur simple si pas de prefab
                    AddSeparator();
                }

                // Afficher le joueur courant avec une couleur distincte
                AddLeaderboardEntry(
                    result.currentPlayer.rank,
                    result.currentPlayer.displayName,
                    (int)result.currentPlayer.score,
                    null,
                    new Color(0.2f, 0.6f, 1f, 1f) // Bleu
                );
            }

            Debug.Log($"[Leaderboard] Displayed {result.topPlayers.Count} entries");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Leaderboard] Error: {e.Message}");
        }
    }

    /// <summary>
    /// Retourne une couleur selon le rang (or, argent, bronze)
    /// </summary>
    private Color? GetRankColor(int rank)
    {
        return rank switch
        {
            1 => new Color(1f, 0.84f, 0f, 1f),      // Or
            2 => new Color(0.75f, 0.75f, 0.75f, 1f), // Argent
            3 => new Color(0.8f, 0.5f, 0.2f, 1f),   // Bronze
            _ => null
        };
    }

    /// <summary>
    /// Ajoute un séparateur visuel "..."
    /// </summary>
    private void AddSeparator()
    {
        GameObject separator = new GameObject("Separator");
        separator.transform.SetParent(contentParent, false);
        
        var text = separator.AddComponent<TextMeshProUGUI>();
        text.text = "• • •";
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.gray;
        
        var layout = separator.AddComponent<LayoutElement>();
        layout.preferredHeight = 40;
    }

    /// <summary>
    /// Supprime toutes les entrées du leaderboard
    /// </summary>
    public void ClearLeaderboard()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Ajoute une entrée au leaderboard
    /// </summary>
    public void AddLeaderboardEntry(int rank, string playerName, int score, Sprite userIcon = null, Color? backgroundColor = null)
    {
        GameObject newEntry = Instantiate(leaderboardElementPrefab, contentParent);
        LeaderboardElement element = newEntry.GetComponent<LeaderboardElement>();
        element.SetData(rank, playerName, score, userIcon, backgroundColor);
    }

    /// <summary>
    /// Données de test
    /// </summary>
    public void DummyPopulate()
    {
        ClearLeaderboard();
        AddLeaderboardEntry(1, "Adamasploots", 1500, null, GetRankColor(1));
        AddLeaderboardEntry(2, "Offieks", 1200, null, GetRankColor(2));
        AddLeaderboardEntry(3, "Loris", 1000, null, GetRankColor(3));
        AddLeaderboardEntry(4, "Fhystel", 800);
        AddLeaderboardEntry(5, "Maitr", 600);
        AddLeaderboardEntry(6, "Tim", 400);
        AddLeaderboardEntry(7, "Elisa", 300);
        AddLeaderboardEntry(8, "πrkiroul", 200);
        AddLeaderboardEntry(9, "Yamatinou", 100);
        AddLeaderboardEntry(10, "RJ", 50);
        
        // Simulation joueur courant hors top 10
        AddSeparator();
        AddLeaderboardEntry(42, "Vous", 15, null, new Color(0.2f, 0.6f, 1f, 1f));
    }
}