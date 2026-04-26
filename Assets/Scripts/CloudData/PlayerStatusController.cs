using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Economy;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

public class PlayerStatusController : MonoBehaviour
{
    private static void ConfigureDiagnosticLogging()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (_diagnosticLoggingConfigured) return;
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        _diagnosticLoggingConfigured = true;
    }
    private static bool _diagnosticLoggingConfigured;
#else
    }
#endif

    // In WebGL builds, emit diagnostics as warnings so they stay visible with stricter console filters.
    private static void LogDiag(string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogWarning(message);
#else
        Debug.Log(message);
#endif
    }

    private static NotificationSystem Notif => NotificationSystem.Instance;

    private async void Start()
    {
        ConfigureDiagnosticLogging();

        if (!AuthenticationService.Instance.IsSignedIn || AuthenticationService.Instance.IsExpired)
        {
            Debug.LogWarning("[PlayerStatusController] Session invalide ou expirée. Redirection vers login.");
            AuthenticationService.Instance.SignOut();
            SceneManager.LoadScene(SceneNames.Login);
            return;
        }

        try
        {
            Notif?.ShowLoading(8);

            await PlayerProfileStore.LoadPackCollectionAsync();
            Notif?.IncrementLoadingStep(); // 1/6

            await PlayerProfileStore.LoadCardCollectionAsync();
            Notif?.IncrementLoadingStep(); // 2/8

            await PlayerProfileStore.LoadPhysicalCardCollectionAsync();
            Notif?.IncrementLoadingStep(); // 3/8

            await PlayerProfileStore.LoadDeckSelectionAsync();
            Notif?.IncrementLoadingStep(); // 4/8

            var displayName = await PlayerProfileStore.LoadDisplayNameAsync();
            if (displayName != null) PlayerProfileStore.DISPLAY_NAME = displayName;
            await PlayerProfileStore.LoadDepartmentAsync();
            await PlayerProfileStore.LoadFriendsAsync();
            Notif?.IncrementLoadingStep(); // 5/8

            await RefreshStatusAsync();
            Notif?.IncrementLoadingStep(); // 6/8

            _ = ResolveBetsOnLoginAsync(); // non-bloquant
            Notif?.IncrementLoadingStep(); // 7/8

            Notif?.IncrementLoadingStep(); // 8/8
            Notif?.HideLoading();

            await StartTutorialIfNeededAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerStatusController] Erreur dans le pipeline de démarrage : {ex.Message}");
            Debug.LogException(ex);
        }
    }

    
    private async Task StartTutorialIfNeededAsync()
    {
        TutorialManager tutorialManager = null;

        for (int i = 0; i < 20; i++)
        {
            tutorialManager = TutorialManager.Instance;
            if (tutorialManager == null)
            {
                tutorialManager = FindFirstObjectByType<TutorialManager>();
            }

            if (tutorialManager != null)
                break;

            await Task.Delay(100);
        }

        if (tutorialManager == null)
        {
            Debug.LogWarning("[PlayerStatusController] TutorialManager not found, skipping auto-start trigger");
            return;
        }

        if (!tutorialManager.autoStartOnFirstLogin)
        {
            LogDiag("[PlayerStatusController] autoStartOnFirstLogin disabled");
            return;
        }

        if (tutorialManager.IsFirstTime())
        {
            LogDiag("[PlayerStatusController] Starting first-time tutorial");
            tutorialManager.StartFirstTimeTutorial();
        }
        else
        {
            LogDiag("[PlayerStatusController] Not first-time user, tutorial auto-start skipped");
        }
    }


    private async Task ResolveBetsOnLoginAsync()
    {
        try
        {
            var events = await EventsRemoteConfig.GetEventsAsync();
            LogDiag($"[ResolveBets] Fetched {events.Length} events");
            
            var res = await BetsClient.ResolveBetsAsync(events);
            
            LogDiag($"[ResolveBets] Response: ok={res?.ok}, resolved={res?.resolved?.Length ?? 0}");
            if (res != null && res.resolved != null)
            {
                foreach (var r in res.resolved)
                {
                    LogDiag($"[ResolveBets] Resolved bet: {r.eventId}, win={r.win}, refund={r.refund}");
                }
            }

            if (res != null && res.ok && res.resolved != null && res.resolved.Length > 0)
            {
                foreach (var r in res.resolved)
                {
                    // Retrouver le titre de l'event si disponible
                    var ev = System.Array.Find(events, e => e.id == r.eventId);
                    var eventLabel = !string.IsNullOrEmpty(ev?.title) ? ev.title : r.eventId;

                    string msg;
                    if (r.win)
                        msg = $"🎉 Pari gagné !\n\"{eventLabel}\"\n+{r.refund} TOKEN crédités";
                    else
                        msg = $"Pari perdu.\n\"{eventLabel}\"\n{r.refund} TOKEN remboursés (50%)";

                    Notif?.ShowNotification(msg);
                }

                // Refresh status after resolving bets to show updated TOKEN balance
                await RefreshStatusAsync();
            }
            else
            {
                LogDiag("[ResolveBets] No bets to resolve or response was not ok");
                if (res != null && !res.ok)
                {
                    Debug.LogWarning($"[ResolveBets] Error: {res.message}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            Notif?.ShowNotification("Impossible de résoudre les paris pour le moment.");
        }
    }

    public async Task RefreshStatusAsync()
    {
        // TOKEN depuis Economy
        var currencies = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        var tokenBal = currencies.Balances.FirstOrDefault(b => b.CurrencyId == "TOKEN");
        var tokens = tokenBal?.Balance ?? 0;
        var collectionPointsBal = currencies.Balances.FirstOrDefault(b => b.CurrencyId == "PC");
        var collectionPoints = collectionPointsBal?.Balance ?? 0;

        // Les setters de PlayerProfileStore notifient automatiquement tous les UpdateDataUI abonnés
        PlayerProfileStore.TOKEN = tokens;
        await PlayerProfileStore.ComputePC();
    }
}

