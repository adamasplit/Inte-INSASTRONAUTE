using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
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
    GameObject tutorialPromptRoot;

    public void OnClick()
    {
        if (!forceShowTutorialPrompt && HasSeenNewGameTutorialPrompt())
        {
            characterSelectUI?.Show();
            return;
        }

        ShowTutorialPrompt(
            "Voulez-vous lancer le tutoriel avant de commencer une nouvelle partie ?",
            StartTutorialFromNewGame,
            characterSelectUI.Show
        );
    }

    void Start()
    {
        WireTutorialPromptButtons();
        RefreshLoadButtonState();
        HideTutorialPrompt();
    }

    void OnEnable()
    {
        WireTutorialPromptButtons();
        RefreshLoadButtonState();
        HideTutorialPrompt();
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
        if (!STSRunSaveSystem.HasLoadableSave())
            return;

        if (RunManager.Instance == null)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
        }

        await STSCardDatabase.LoadAsync();

        if (!RunManager.Instance.LoadSavedRun())
            return;

        STSSceneLoader.Instance?.LoadScene(resumeSceneName);
    }

    public void StartTutorialFromNewGame()
    {
        Debug.Log("Starting tutorial from new game.");
        MarkNewGameTutorialPromptSeen();
        HideTutorialPrompt();

        if (RunManager.Instance == null)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
        }

        RunManager.Instance.StartTutorialRun();
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