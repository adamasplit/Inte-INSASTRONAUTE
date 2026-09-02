using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Threading.Tasks;
using TMPro;

public class STSMainMenuController : MonoBehaviour
{
    public TMP_Text gameButtonText;
    public Button loadButton;
    public Button pvpButton;
    public string resumeSceneName = "STS_Map";
    public string bootSceneName = "STS_Boot";
    public string pvpMenuSceneName = "STS_MultiplayerMenu";
    const string NewGameTutorialPromptKey = "STS_NewGameTutorialPromptSeen";

    /// <summary>
    /// Ce qui retient que l'invite du tutoriel a déjà été posée au moins une fois.
    ///
    /// <para>Distinct de <c>NewGameTutorialPromptKey</c>, que seul un « oui » pose : celle-ci
    /// enregistre simplement que la question a été montrée, quelle qu'ait été la réponse.
    /// C'est ce qui décide si « revoir le tutoriel » veut dire quelque chose.</para>
    /// </summary>
    const string TutorialPromptOfferedKey = "STS_TutorialPromptOffered";
    public Button tutorialButton;
    public string tutorialButtonLabel = "Revoir le tutoriel";

    /// <summary>
    /// Le bouton qui ouvre le menu de combat de débogage.
    ///
    /// <para>Le panneau qu'il ouvre est branché dans la scène ; on ne s'occupe ici que de
    /// savoir s'il a le droit d'exister sur cet écran.</para>
    /// </summary>
    public Button debugButton;
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
    bool tutorialButtonWired;
    bool transitionInProgress;
    int overlayFadeVersion;
    int loadButtonRefreshVersion;
    int debugButtonRefreshVersion;

    void Awake()
    {
        if (introSequence == null)
        {
            introSequence = FindObjectOfType<STSMainMenuIntroSequence>(true);
        }

        ResetBlackOverlay();
        EnsureButtonGoldGlow(loadButton);
        EnsureButtonGoldGlow(pvpButton);

        // Caché avant même la première image : la réponse du serveur arrive une poignée de
        // trames plus tard, et un bouton de débogage qui clignote sur l'écran d'un joueur
        // ordinaire est déjà un bouton de trop.
        if (debugButton != null)
        {
            debugButton.gameObject.SetActive(false);
        }
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
            STSSceneLoader.Instance?.LoadScene(RunManager.Instance.ResolveRemoteResumeScene());
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
        RefreshDebugButtonState();
        HideTutorialPrompt();
        EnsureTutorialButton();
        introSequence?.Play();
    }

    void OnEnable()
    {
        ResetBlackOverlay();
        WireTutorialPromptButtons();
        RefreshLoadButtonState();
        RefreshDebugButtonState();
        HideTutorialPrompt();
        EnsureTutorialButton();
        EnsureButtonGoldGlow(loadButton);
        EnsureButtonGoldGlow(pvpButton);
    }

    /// <summary>
    /// N'affiche le bouton de débogage que si le serveur laisserait vraiment passer celui qui
    /// le cliquerait.
    ///
    /// <para>Deux choses peuvent le fermer, et le serveur est seul à les connaître : le drapeau
    /// <c>app.sts.debug.combat.enabled</c>, sans lequel la route n'est pas déployée du tout, et
    /// le rôle de l'appelant, la route n'étant ouverte qu'à l'encadrement. Un bouton affiché
    /// sans l'un des deux ne mène qu'à un panneau dont le lancement échoue.</para>
    ///
    /// <para>Comme <see cref="RefreshLoadButtonState"/>, garde le numéro de la demande : ce
    /// menu est réactivé à chaque retour à l'accueil, et la réponse d'un appel abandonné ne
    /// doit pas rallumer un bouton que le suivant vient d'éteindre.</para>
    /// </summary>
    public async void RefreshDebugButtonState()
    {
        if (debugButton == null)
        {
            return;
        }

        int refreshVersion = ++debugButtonRefreshVersion;
        debugButton.gameObject.SetActive(false);

        bool available = await STSApiClient.IsDebugCombatAvailableAsync();

        if (refreshVersion != debugButtonRefreshVersion || debugButton == null)
        {
            return;
        }

        debugButton.gameObject.SetActive(available);
        if (available)
        {
            EnsureButtonGoldGlow(debugButton);
        }
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
        if (hasCurrentRun)
        {
            gameButtonText.text="Reprendre la partie";
        }
        else
        {
            gameButtonText.text="Nouvelle partie";
        }
    }

    public async void LoadSavedRun()
    {
        await AbandonCurrentRunAsync();
    }

    public async void OnClickPvp()
    {
        if (transitionInProgress)
        {
            return;
        }

        transitionInProgress = true;
        await FadeBlackOverlayToAsync(1f, blackFadeInDuration, keepVisibleAtEnd: true);

        introSequence?.HideTitleLine();

        STSSceneLoader.Instance?.LoadScene(pvpMenuSceneName);

        transitionInProgress = false;
    }

    public async Task AbandonCurrentRunAsync()
    {
        if (transitionInProgress)
        {
            return;
        }

        transitionInProgress = true;
        try
        {
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
        }
        finally
        {
            transitionInProgress = false;
        }
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

    public async void OnClickTutorial()
    {
        if (transitionInProgress)
        {
            return;
        }

        transitionInProgress = true;
        await FadeBlackOverlayToAsync(1f, blackFadeInDuration, keepVisibleAtEnd: true);

        ShowTutorialPrompt(
            "Voulez-vous revoir le tutoriel ?",
            StartTutorialFromNewGame,
            HandleDeclineTutorialReplay
        );

        transitionInProgress = false;
    }

    async void HandleDeclineTutorialReplay()
    {
        await FadeBlackOverlayToAsync(0f, blackFadeOutDuration);
    }

    /// <summary>
    /// Crée et montre le bouton « revoir le tutoriel », mais seulement une fois l'invite posée.
    ///
    /// <para>À qui n'a jamais vu la question, le bouton propose de revoir quelque chose dont il
    /// n'a jamais entendu parler. La première partie s'en charge : elle demande, et c'est à
    /// partir de là que « revoir » veut dire quelque chose. <c>forceShowTutorialPrompt</c> le
    /// rouvre pour l'essayer sans avoir à effacer les PlayerPrefs.</para>
    /// </summary>
    void EnsureTutorialButton()
    {
        if (!HasBeenOfferedTutorial() && !forceShowTutorialPrompt)
        {
            if (tutorialButton != null)
            {
                tutorialButton.gameObject.SetActive(false);
            }
            return;
        }

        if (tutorialButton == null)
        {
            Button template = pvpButton != null ? pvpButton : loadButton;
            if (template == null)
            {
                Debug.LogWarning("Cannot create tutorial button: no template button available.");
                return;
            }

            GameObject clone = Instantiate(template.gameObject, template.transform.parent);
            clone.name = "TutorialButton";
            clone.SetActive(true);

            tutorialButton = clone.GetComponent<Button>();
            tutorialButton.onClick = new Button.ButtonClickedEvent();

            clone.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);

            RectTransform templateRect = template.GetComponent<RectTransform>();
            RectTransform cloneRect = clone.GetComponent<RectTransform>();
            bool parentHasLayout = template.transform.parent != null
                && template.transform.parent.GetComponent<LayoutGroup>() != null;
            if (!parentHasLayout && templateRect != null && cloneRect != null)
            {
                cloneRect.anchoredPosition = templateRect.anchoredPosition
                    - new Vector2(0f, templateRect.rect.height + 12f);
            }
        }

        tutorialButton.gameObject.SetActive(true);
        SetButtonLabel(tutorialButton, tutorialButtonLabel);
        EnsureButtonGoldGlow(tutorialButton);

        if (!tutorialButtonWired)
        {
            tutorialButton.onClick.AddListener(OnClickTutorial);
            tutorialButtonWired = true;
        }
    }

    void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
        }
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

        // La question est posée : le bouton a désormais un sens. On le rafraîchit tout de
        // suite plutôt qu'au prochain OnEnable, pour le cas où le joueur refuse et revient
        // au menu sans que la scène ait été rechargée.
        MarkTutorialOffered();
        EnsureTutorialButton();
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

    bool HasBeenOfferedTutorial()
    {
        return PlayerPrefs.GetInt(TutorialPromptOfferedKey, 0) == 1;
    }

    /// <summary>
    /// Posé quand l'invite s'affiche, pas quand elle est acceptée : refuser le tutoriel reste
    /// l'avoir vu proposer, et c'est tout ce que le bouton demande pour avoir du sens.
    /// </summary>
    void MarkTutorialOffered()
    {
        if (HasBeenOfferedTutorial())
        {
            return;
        }

        PlayerPrefs.SetInt(TutorialPromptOfferedKey, 1);
        PlayerPrefs.Save();
    }

    void MarkNewGameTutorialPromptSeen()
    {
        PlayerPrefs.SetInt(NewGameTutorialPromptKey, 1);
        PlayerPrefs.Save();
    }

}
