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
            GameObject btnObj = Instantiate(characterButtonPrefab, characterListContainer);
            CharacterSelectButton btn = btnObj.GetComponent<CharacterSelectButton>();
            btn.Init(character, OnCharacterSelected);
        }
        PlayersDatabase.Load();
        OnCharacterSelected(SelectableCharacter.EP);
        characterListContainer.position= new Vector3(2000, characterListContainer.position.y, 0);
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
        backgroundImage.color= character switch
        {
            SelectableCharacter.EP => new Color(0.8f,0.1f,0.1f,backgroundImage.color.a),
            SelectableCharacter.MECA => new Color(0.1f,0.8f,0.1f,backgroundImage.color.a),
            SelectableCharacter.CFI => new Color(0.1f,0.1f,0.8f,backgroundImage.color.a),
            SelectableCharacter.GM => new Color(0.8f,0.8f,0.1f,backgroundImage.color.a),
            SelectableCharacter.ITI => new Color(0.8f,0.1f,0.8f,backgroundImage.color.a),
            SelectableCharacter.GC => new Color(0.1f,0.8f,0.8f,backgroundImage.color.a),
            SelectableCharacter.AI => new Color(0.5f,0.5f,0.5f,backgroundImage.color.a),
            SelectableCharacter.PERF => new Color(0.9f,0.5f,0.1f,backgroundImage.color.a),
            SelectableCharacter.MRIE => new Color(0.5f,0.1f,0.9f,backgroundImage.color.a),
            _ => Color.white
        };
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