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

        if (!forceShowTutorialPrompt && HasSeenNewGameTutorialPrompt())
        {
            characterSelectUI?.Show();
            await FadeBlackOverlayToAsync(0f, blackFadeOutDuration);
            transitionInProgress = false;
            return;
        }

        ShowTutorialPrompt(
            "Voulez-vous lancer le tutoriel avant de commencer une nouvelle partie ?",
            StartTutorialFromNewGame,
            HandleDeclineTutorialPrompt
        );

        transitionInProgress = false;
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

    public void RefreshLoadButtonState()
    {
        if (loadButton != null)
        {
            loadButton.gameObject.SetActive(STSRunSaveSystem.HasLoadableSave());
        }
    }

    public async void LoadSavedRun()
    {
        if (transitionInProgress)
        {
            return;
        }

        if (!STSRunSaveSystem.HasLoadableSave())
            return;

        transitionInProgress = true;
        await FadeBlackOverlayToAsync(1f, blackFadeInDuration, keepVisibleAtEnd: true);

        introSequence?.HideTitleLine();

        if (RunManager.Instance == null)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
        }

        await STSCardDatabase.LoadAsync();

        if (!RunManager.Instance.LoadSavedRun())
        {
            await FadeBlackOverlayToAsync(0f, blackFadeOutDuration);
            transitionInProgress = false;
            return;
        }

        STSSceneLoader.Instance?.LoadScene(resumeSceneName);
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