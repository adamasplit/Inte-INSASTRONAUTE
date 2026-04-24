using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public class LeaderboardBinder : MonoBehaviour
{
    [SerializeField] private LeaderboardController leaderboardController;
    [SerializeField] private Button refreshButton;

    private static NotificationSystem Notif => NotificationSystem.Instance;

    void Start()
    {
        if (refreshButton) refreshButton.onClick.AddListener(() => _ = RefreshLeaderboardAsync());
        _ = RefreshLeaderboardAsync();
    }

    void OnDestroy()
    {
        if (refreshButton) refreshButton.onClick.RemoveAllListeners();
    }

    public async Task RefreshLeaderboardAsync()
    {
        if (leaderboardController == null)
        {
            Debug.LogError("[LeaderboardBinder] leaderboardController non assigné.");
            return;
        }

        if (refreshButton) refreshButton.interactable = false;

        try
        {
            await leaderboardController.RefreshLeaderboardAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LeaderboardBinder] Erreur rafraîchissement : {ex.Message}");
            Notif?.ShowNotification("Impossible de charger le classement. Réessaie plus tard.");
        }
        finally
        {
            if (refreshButton) refreshButton.interactable = true;
        }
    }
}
