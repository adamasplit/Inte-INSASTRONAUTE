using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Threading.Tasks;
using TMPro;

public class STSMainMenuController : MonoBehaviour
{
    public Button loadButton;
    public string resumeSceneName = "STS_Map";
    public string bootSceneName = "STS_Boot";
    const string NewGameTutorialPromptKey = "STS_NewGameTutorialPromptSeen";
    public GameObject tutorialPromptPanel;
    public Button acceptTutorialButton;
    public Button declineTutorialButton;
    public bool forceShowTutorialPrompt = false;
    public CharacterSelectUI characterSelectUI;
    public STSMainMenuIntroSequence introSequence;
    public CanvasGroup blackTransitionOverlay;
    public float blackFadeInDuration = 0.3f;
    public float blackFadeOutDuration = 0.25f;
    GameObject tutorialPromptRoot;
    bool transitionInProgress;
    int overlayFadeVersion;
    int loadButtonRefreshVersion;

    void Awake()
    {
        if (introSequence == null)
        {
            introSequence = FindObjectOfType<STSMainMenuIntroSequence>(true);
        }

        ResetBlackOverlay();
        EnsureButtonGoldGlow(loadButton);
    }

    public async void OnClick()
    {
        if (transitionInProgress)
        {
            return;
        }

        transitionInProgress = true;
        await FadeBlackOverlayToAsync(1f, blackFadeInDuration, keepVisibleAtEnd: true);

        if (await TryContinueExistingRunAsync())
        {
            transitionInProgress = false;
            return;
        }

        if (!forceShowTutorialPrompt && HasSeenNewGameTutorialPrompt())
        {
            characterSelectUI?.Show();
            await FadeBlackOverlayToAsync(0f, blackFadeOutDuration);
            transitionInProgress = false;
            return;
        }

        ShowTutorialPrompt(
            "Voulez-vous lancer le tutoriel avant de commencer ou reprendre une partie ?",
            StartTutorialFromNewGame,
            HandleDeclineTutorialPrompt
        );

        transitionInProgress = false;
    }

    async Task<bool> TryContinueExistingRunAsync()
    {
        try
        {
            STSApiCurrentRunResponse currentRun = await STSApiClient.CurrentRunAsync();
            if (currentRun == null || !currentRun.hasRun || currentRun.run == null)
            {
                return false;
            }

            introSequence?.HideTitleLine();

            if (RunManager.Instance == null)
            {
                new GameObject("RunManager").AddComponent<RunManager>();
            }

            RunManager.Instance.OnRunEnd(true, false);

            await STSCardDatabase.LoadAsync();
            await PlayersDatabase.LoadAsync();
            await EnemyDataDatabase.LoadAsync();
            await EnemyPoolDatabase.LoadAsync();

            if (!RunManager.Instance.ApplyRemoteRunIfAvailable(currentRun.run))
            {
                return false;
            }

            if (RunManager.Instance.ui != null)
            {
                RunManager.Instance.ui.gameObject.SetActive(true);
            }

            STSRunAuditSystem.RecordRunStarted(RunManager.Instance);
            STSSceneLoader.Instance?.LoadScene(resumeSceneName);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to continue existing run from main menu: {ex.Message}");
            return false;
        }
    }

    void Start()
    {
        ResetBlackOverlay();
        WireTutorialPromptButtons();
        RefreshLoadButtonState();
        HideTutorialPrompt();
        introSequence?.Play();
    }

    void OnEnable()
    {
        ResetBlackOverlay();
        WireTutorialPromptButtons();
        RefreshLoadButtonState();
        HideTutorialPrompt();
        EnsureButtonGoldGlow(loadButton);
    }

    public async void RefreshLoadButtonState()
    {
        if (loadButton == null)
        {
            return;
        }

        int refreshVersion = ++loadButtonRefreshVersion;

        bool hasCurrentRun = false;

        if (RunManager.Instance != null && !string.IsNullOrWhiteSpace(RunManager.Instance.runId))
        {
            hasCurrentRun = true;
        }
        else if (STSRunSaveSystem.TryGetSavedRunId(out _))
        {
            hasCurrentRun = true;
        }
        else
        {
            try
            {
                STSApiCurrentRunResponse currentRun = await STSApiClient.CurrentRunAsync();
                if (currentRun == null)
                {
                    hasCurrentRun = false;
                }
                else
                {
                    hasCurrentRun = currentRun.hasRun && currentRun.run != null && !string.IsNullOrWhiteSpace(currentRun.run.runId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to refresh Give Up button state from API: {ex.Message}");
                hasCurrentRun = false;
            }
        }

        if (refreshVersion != loadButtonRefreshVersion)
        {
            return;
        }

        loadButton.gameObject.SetActive(hasCurrentRun);
    }

    public async void LoadSavedRun()
    {
        await AbandonCurrentRunAsync();
    }

    public async Task AbandonCurrentRunAsync()
    {
        if (transitionInProgress)
        {
            return;
        }

        transitionInProgress = true;
        await FadeBlackOverlayToAsync(1f, blackFadeInDuration, keepVisibleAtEnd: true);

        introSequence?.HideTitleLine();

        string runId = null;
        if (RunManager.Instance != null && !string.IsNullOrWhiteSpace(RunManager.Instance.runId))
        {
            runId = RunManager.Instance.runId;
        }
        else if (STSRunSaveSystem.TryGetSavedRunId(out string savedRunId))
        {
            runId = savedRunId;
        }
        else
        {
            try
            {
                STSApiCurrentRunResponse currentRun = await STSApiClient.CurrentRunAsync();
                runId = currentRun != null && currentRun.hasRun ? currentRun.run?.runId : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to query current run before abandon: {ex.Message}");
            }
        }

        if (!string.IsNullOrWhiteSpace(runId))
        {
            try
            {
                await STSApiClient.ResetRunAsync(runId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to reset remote run during abandon: {ex.Message}");
            }
        }

        if (RunManager.Instance == null)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
        }

        RunManager.Instance.OnRunEnd(true, false);
        RefreshLoadButtonState();

        await FadeBlackOverlayToAsync(0f, blackFadeOutDuration);
        transitionInProgress = false;
    }

    public void StartTutorialFromNewGame()
    {
        Debug.Log("Starting tutorial from new game.");
        introSequence?.HideTitleLine();
        MarkNewGameTutorialPromptSeen();
        HideTutorialPrompt();

        if (RunManager.Instance == null)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
        }

        RunManager.Instance.StartTutorialRun();
    }

    async void HandleDeclineTutorialPrompt()
    {
        characterSelectUI?.Show();
        await FadeBlackOverlayToAsync(0f, blackFadeOutDuration);
    }

    async Task FadeBlackOverlayToAsync(float targetAlpha, float duration, bool keepVisibleAtEnd = false)
    {
        if (blackTransitionOverlay == null)
        {
            return;
        }

        int version = ++overlayFadeVersion;
        float startAlpha = blackTransitionOverlay.alpha;
        float clampedTarget = Mathf.Clamp01(targetAlpha);
        float safeDuration = Mathf.Max(0f, duration);

        blackTransitionOverlay.gameObject.SetActive(true);
        blackTransitionOverlay.interactable = false;
        blackTransitionOverlay.blocksRaycasts = true;

        if (safeDuration <= 0.0001f)
        {
            blackTransitionOverlay.alpha = clampedTarget;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                if (version != overlayFadeVersion)
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                blackTransitionOverlay.alpha = Mathf.Lerp(startAlpha, clampedTarget, t);
                await Task.Yield();
            }

            if (version != overlayFadeVersion)
            {
                return;
            }

            blackTransitionOverlay.alpha = clampedTarget;
        }

        blackTransitionOverlay.blocksRaycasts = false;

        if (clampedTarget <= 0.001f && !keepVisibleAtEnd)
        {
            blackTransitionOverlay.gameObject.SetActive(false);
        }
    }

    void ResetBlackOverlay()
    {
        overlayFadeVersion++;
        transitionInProgress = false;

        if (blackTransitionOverlay == null)
        {
            return;
        }

        blackTransitionOverlay.alpha = 0f;
        blackTransitionOverlay.interactable = false;
        blackTransitionOverlay.blocksRaycasts = false;
        blackTransitionOverlay.gameObject.SetActive(false);
    }

    void ShowTutorialPrompt(string message, Action yesAction, Action noAction)
    {
        if (tutorialPromptPanel == null)
        {
            Debug.LogWarning("Tutorial prompt panel is not assigned.");
            yesAction?.Invoke();
            return;
        }

        tutorialPromptRoot = tutorialPromptPanel;
        SetPromptMessage(message);
        tutorialPromptPanel.SetActive(true);
        ConfigurePromptButton(acceptTutorialButton, () =>
        {
            HideTutorialPrompt();
            yesAction?.Invoke();
        });

        ConfigurePromptButton(declineTutorialButton, () =>
        {
            HideTutorialPrompt();
            noAction?.Invoke();
        });
    }

    void HideTutorialPrompt()
    {
        if (tutorialPromptPanel != null)
        {
            tutorialPromptPanel.SetActive(false);
        }
    }

    void EnsureButtonGoldGlow(Button button)
    {
        if (button == null)
        {
            return;
        }

        if (button.GetComponent<STSButtonGoldGlow>() == null)
        {
            button.gameObject.AddComponent<STSButtonGoldGlow>();
        }
    }

    void WireTutorialPromptButtons()
    {
        if (acceptTutorialButton != null)
        {
            acceptTutorialButton.enabled = false;
        }

        if (declineTutorialButton != null)
        {
            declineTutorialButton.enabled = false;
        }
    }

    void ConfigurePromptButton(Button button, Action onClick)
    {
        if (button == null)
        {
            return;
        }

        PromptButtonRelay relay = button.GetComponent<PromptButtonRelay>();
        if (relay == null)
        {
            relay = button.gameObject.AddComponent<PromptButtonRelay>();
        }

        relay.Bind(onClick);
        button.enabled = false;
    }

    sealed class PromptButtonRelay : MonoBehaviour, IPointerClickHandler
    {
        Action onClick;

        public void Bind(Action action)
        {
            onClick = action;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onClick?.Invoke();
        }
    }

    void SetPromptMessage(string message)
    {
        if (tutorialPromptPanel == null)
            return;

        TMP_Text tmpText = tutorialPromptPanel.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = message;
            return;
        }

        Text legacyText = tutorialPromptPanel.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = message;
        }
    }

    bool HasSeenNewGameTutorialPrompt()
    {
        return PlayerPrefs.GetInt(NewGameTutorialPromptKey, 0) == 1;
    }

    void MarkNewGameTutorialPromptSeen()
    {
        PlayerPrefs.SetInt(NewGameTutorialPromptKey, 1);
        PlayerPrefs.Save();
    }

}