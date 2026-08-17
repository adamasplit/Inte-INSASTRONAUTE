using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
public class CharacterSelectUI : MonoBehaviour
{
    public GameObject selectPanel;
    public Image background;
    public List<Sprite> possibleBackgrounds;
    public Transform characterListContainer;
    public GameObject characterButtonPrefab;
    public CircularMenu circularMenu;
    public Button confirmButton;
    public Image backgroundImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI characterDescriptionText;
    public TextMeshProUGUI relicDescriptionText;
    public Relic relic;
    public RectTransform boxContainer;
    public STSMainMenuIntroSequence introSequence;
    public float backgroundColorTransitionDuration = 0.28f;
    public Image characterIllustration;
    public float illustrationSlideDuration = 0.45f;
    Coroutine backgroundColorTransitionRoutine;
    Coroutine illustrationSlideRoutine;
    RectTransform illustrationRect;
    Vector2 illustrationHomePosition;
    async void Awake()
    {
        if (introSequence == null)
        {
            introSequence = FindObjectOfType<STSMainMenuIntroSequence>(true);
        }

        if (characterIllustration != null)
        {
            illustrationRect = characterIllustration.rectTransform;
            illustrationHomePosition = illustrationRect.anchoredPosition;
        }

        STSSceneLoader.Instance?.BeginLoading();

        try
        {
            foreach (SelectableCharacter character in System.Enum.GetValues(typeof(SelectableCharacter)))
            {
                if (character == SelectableCharacter.Aucun|| character == SelectableCharacter.Impossible|| character == SelectableCharacter.Starting) continue;
                GameObject btnObj = Instantiate(characterButtonPrefab, characterListContainer);
                btnObj.name = character.ToString();
                CharacterSelectButton btn = btnObj.GetComponent<CharacterSelectButton>();
                btn.Init(character, OnCharacterSelected);
            }

            await PlayersDatabase.LoadAsync();
            OnCharacterSelected(SelectableCharacter.EP);
            circularMenu.Init();
            Hide();
        }
        finally
        {
            STSSceneLoader.Instance?.EndLoading();
            STSSceneLoader.Instance?.SceneReady();
        }
    }
    public void Show()
    {
        selectPanel.SetActive(true);
    }
    public void Hide()
    {
        selectPanel.SetActive(false);
    }
    public void OnCharacterSelected(SelectableCharacter character)
    {
        circularMenu.ForceToFront(characterListContainer.Find(character.ToString()).GetComponent<RectTransform>());
        //background.sprite = possibleBackgrounds[(int)character];
        foreach (Transform child in characterListContainer)
        {
            CharacterSelectButton btn = child.GetComponent<CharacterSelectButton>();
            btn.Switch(btn.character == character);
        }
        titleText.text = CharacterName(character);
        characterDescriptionText.text = CharacterDescription(character);
        LayoutRebuilder.ForceRebuildLayoutImmediate(boxContainer);
        relic = character switch
        {
            SelectableCharacter.EP => new EPRelic(),
            SelectableCharacter.MECA => new MECARelic(),
            SelectableCharacter.CFI => new CFIRelic(),
            SelectableCharacter.GM => new GMRelic(),
            SelectableCharacter.ITI => new ITIRelic(),
            SelectableCharacter.GC => new GCRelic(),
            SelectableCharacter.AI => new AIRelic(),
            SelectableCharacter.PERF => new PERFRelic(),
            SelectableCharacter.MRIE => new MRIERelic(),
            _ => null
        };
        relicDescriptionText.text = $"<color=yellow>{relic.name}</color>\n";
        relicDescriptionText.text += relic.Describe();
        Color col=SelectableCharacterUtils.getCharacterColor(character);
        StartBackgroundColorTransition(new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 1f));
        ShowCharacterIllustration(character);
        EnsureButtonGoldGlow(confirmButton);
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() => OnCharacterConfirm(character));
    }
    public void OnCharacterConfirm(SelectableCharacter character)
    {
        if (relic == null)
        {
            Debug.LogError($"No relic assigned for character {character}. Cannot start run.");
            return;
        }
        introSequence?.HideTitleLine();
        RunManager.Instance.forceTutorial = false;
        int hp = PlayersDatabase.Get(character)?.hp ?? 100;
        RunManager.Instance.StartRun(character.ToString(), hp, new List<Relic>() {relic}, preferFreshRun: true);
    }

    public string CharacterDescription(SelectableCharacter character)
    {
        string result = PlayersDatabase.Get(character)?.description ?? "Aucune description disponible.";
        return result;
    }
    public string CharacterName(SelectableCharacter character)
    {
        string result=PlayersDatabase.Get(character)?.characterName ?? "Inconnu";
        return $"<color=yellow>{result}</color>\n";
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

    void StartBackgroundColorTransition(Color targetColor)
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (backgroundColorTransitionRoutine != null)
        {
            StopCoroutine(backgroundColorTransitionRoutine);
            backgroundColorTransitionRoutine = null;
        }

        if (backgroundColorTransitionDuration <= 0f)
        {
            backgroundImage.color = targetColor;
            return;
        }

        backgroundColorTransitionRoutine = StartCoroutine(AnimateBackgroundColor(targetColor));
    }

    IEnumerator AnimateBackgroundColor(Color targetColor)
    {
        Color startColor = backgroundImage.color;
        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, backgroundColorTransitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            backgroundImage.color = Color.Lerp(startColor, targetColor, eased);
            yield return null;
        }

        backgroundImage.color = targetColor;
        backgroundColorTransitionRoutine = null;
    }

    void ShowCharacterIllustration(SelectableCharacter character)
    {
        if (characterIllustration == null)
        {
            return;
        }

        Sprite sprite = Resources.Load<Sprite>($"STS/UI/{character}");
        if (illustrationSlideRoutine != null)
        {
            StopCoroutine(illustrationSlideRoutine);
            illustrationSlideRoutine = null;
        }

        if (sprite == null)
        {
            characterIllustration.gameObject.SetActive(false);
            return;
        }

        characterIllustration.sprite = sprite;
        characterIllustration.gameObject.SetActive(true);

        if (illustrationRect == null)
        {
            return;
        }

        Canvas canvas = characterIllustration.canvas;
        float offscreenX = illustrationHomePosition.x + (canvas != null ? canvas.GetComponent<RectTransform>().rect.width : Screen.width);
        Vector2 startPosition = new Vector2(offscreenX, illustrationHomePosition.y);

        if (illustrationSlideDuration <= 0f)
        {
            illustrationRect.anchoredPosition = illustrationHomePosition;
            return;
        }

        illustrationSlideRoutine = StartCoroutine(AnimateIllustrationSlide(startPosition));
    }

    IEnumerator AnimateIllustrationSlide(Vector2 startPosition)
    {
        illustrationRect.anchoredPosition = startPosition;
        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, illustrationSlideDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            illustrationRect.anchoredPosition = Vector2.Lerp(startPosition, illustrationHomePosition, eased);
            yield return null;
        }

        illustrationRect.anchoredPosition = illustrationHomePosition;
        illustrationSlideRoutine = null;
    }
}