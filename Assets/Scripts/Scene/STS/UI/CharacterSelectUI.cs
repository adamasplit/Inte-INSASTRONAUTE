using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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
    void Awake()
    {
        foreach (SelectableCharacter character in System.Enum.GetValues(typeof(SelectableCharacter)))
        {
            if (character == SelectableCharacter.Aucun|| character == SelectableCharacter.Impossible|| character == SelectableCharacter.Starting) continue;
            GameObject btnObj = Instantiate(characterButtonPrefab, characterListContainer);
            btnObj.name = character.ToString();
            CharacterSelectButton btn = btnObj.GetComponent<CharacterSelectButton>();
            btn.Init(character, OnCharacterSelected);
        }
        PlayersDatabase.Load();
        OnCharacterSelected(SelectableCharacter.EP);
        circularMenu.Init();
        Hide();
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
        background.sprite = possibleBackgrounds[(int)character];
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
        backgroundImage.color= SelectableCharacterUtils.getCharacterColor(character);
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
        int hp = PlayersDatabase.Get(character)?.hp ?? 100;
        RunManager.Instance.StartRun(character.ToString(), hp, new List<Relic>() {relic});
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
}