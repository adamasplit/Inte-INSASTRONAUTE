using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Economy;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

public class PlayerStatusController : MonoBehaviour
{
    private static bool diagnosticLoggingConfigured;

    private static void ConfigureDiagnosticLogging()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (diagnosticLoggingConfigured)
            return;

        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        diagnosticLoggingConfigured = true;
#endif
    }

    // In WebGL builds, emit diagnostics as warnings so they stay visible with stricter console filters.
    private static void LogDiag(string message)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogWarning(message);
#else
        Debug.Log(message);
#endif
    }

    public UpdateDataUI[] uIElements;

    [Header("Refs")]
    [SerializeField] private MainUIBinder ui;
    public CardCollectionController cardCollectionController;
    public PackCollectionController packCollectionController;
    public LoadingScreen loadingScreen;

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
            loadingScreen?.gameObject.SetActive(true);
            loadingScreen?.Initialize(6);

            await PlayerProfileStore.LoadPackCollectionAsync();
            loadingScreen?.IncrementStep(); // 1/6

            await PlayerProfileStore.LoadCardCollectionAsync();
            loadingScreen?.IncrementStep(); // 2/6

            var displayName = await PlayerProfileStore.LoadDisplayNameAsync();
            if (displayName != null) PlayerProfileStore.DISPLAY_NAME = displayName;
            loadingScreen?.IncrementStep(); // 3/6

            await RefreshStatusAsync();
            loadingScreen?.IncrementStep(); // 4/6

            _ = ResolveBetsOnLoginAsync(); // non-bloquant
            loadingScreen?.IncrementStep(); // 5/6

            loadingScreen?.IncrementStep(); // 6/6
            loadingScreen?.gameObject.SetActive(false);

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

    private async Task AllNecessaryComponentsPresentAsync()
    {
        const int maxAttempts = 50; // 5s timeout
        for (int i = 0; i < maxAttempts; i++)
        {
            bool leaderboardReady = FindFirstObjectByType<LeaderboardController>() != null;
            bool shopReady = FindFirstObjectByType<ShopRemoteLoader>() != null;
            bool eventsReady = FindFirstObjectByType<EventsMenuController>() != null;

            if (leaderboardReady && shopReady && eventsReady) return;

            await Task.Delay(100);
        }
        Debug.LogWarning("[PlayerStatusController] AllNecessaryComponentsPresentAsync timed out after 5s.");
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

                    ui.ShowNotification(msg);
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
            ui.ShowNotification("Impossible de résoudre les paris pour le moment.");
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

        PlayerProfileStore.TOKEN = tokens;
        await PlayerProfileStore.ComputePC();

        foreach (var ui in uIElements)
        {
            ui.RefreshDataUI();
        }
    }
}

