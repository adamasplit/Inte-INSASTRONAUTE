using System.Linq;
using UnityEngine;
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
    public LoadingScreen loadingScreen;

    private async void Start()
    {
        ConfigureDiagnosticLogging();
        bool tutorialTriggerRequested = false;

        try
        {
            // Initialize loading screen with total steps
            if (loadingScreen != null)
            {
                loadingScreen.gameObject.SetActive(true);
                loadingScreen.Initialize(8); // Total number of loading steps
            }

            await PlayerProfileStore.LoadPackCollectionAsync();
            loadingScreen?.IncrementStep();
        
            await PlayerProfileStore.LoadCardCollectionAsync();
            loadingScreen?.IncrementStep();

            uIElements = FindObjectsByType<UpdateDataUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            loadingScreen?.IncrementStep();
        
            //await Task.Delay(500); // Attendre un peu pour s'assurer que tout est prêt
            await AllNecessaryComponentsPresentAsync();
            loadingScreen?.IncrementStep();
        
            await RefreshStatusAsync();
            loadingScreen?.IncrementStep();
        
            await FindFirstObjectByType<LeaderboardController>().RefreshLeaderboardAsync();
            loadingScreen?.IncrementStep();
        
            await FindFirstObjectByType<ShopRemoteLoader>().UpdateShopFromRemoteTask();
            loadingScreen?.IncrementStep();
        
            await ResolveBetsOnLoginAsync();
            loadingScreen?.IncrementStep();

            FindFirstObjectByType<CardCollectionController>().RefreshCollection(false,false,true);
            loadingScreen?.IncrementStep();
        
            await FindFirstObjectByType<EventsMenuController>().RefreshEventsAsync();
        
            if (loadingScreen != null)
            {
                loadingScreen.CompleteLoading();
                loadingScreen.HideWithFade();
            }

            // Start tutorial for first-time users after loading is complete
            await Task.Delay(1000); // Wait for loading screen to fade
            tutorialTriggerRequested = true;
            _ = StartTutorialIfNeededAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PlayerStatusController] Startup pipeline failed: {ex.Message}");
            Debug.LogException(ex);
        }
        finally
        {
            if (!tutorialTriggerRequested)
            {
                LogDiag("[PlayerStatusController] Triggering tutorial check from fallback path.");
                _ = StartTutorialIfNeededAsync();
            }
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
        while (FindFirstObjectByType<LeaderboardController>() == null ||
               FindFirstObjectByType<ShopRemoteLoader>() == null ||
               FindFirstObjectByType<EventsMenuController>() == null)
        {
            await Task.Delay(100);
            LogDiag("[PlayerStatusController] Null components");
            if (FindFirstObjectByType<LeaderboardController>() == null)
                LogDiag("[PlayerStatusController] LeaderboardController not found");
            if (FindFirstObjectByType<ShopRemoteLoader>() == null)
                LogDiag("[PlayerStatusController] ShopRemoteLoader not found");
            if (FindFirstObjectByType<EventsMenuController>() == null)
                LogDiag("[PlayerStatusController] EventsMenuController not found");
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
                    var resultText = r.win ? "Gagné" : "Perdu";
                    var sign = r.win ? "+" : "-";
                    ui.ShowNotification($"Pari résolu ({r.eventId}) : {resultText} | {sign}{r.refund} TOKEN");
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

