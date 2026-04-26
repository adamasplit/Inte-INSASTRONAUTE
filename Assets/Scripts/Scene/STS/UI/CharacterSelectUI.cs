using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
public class CharacterSelectUI : MonoBehaviour
{
    public GameObject selectPanel;
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
        OnCharacterSelected(SelectableCharacter.EP);
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
            SelectableCharacter.ARCHI => new ARCHIRelic(),
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
            SelectableCharacter.ARCHI => new Color(0.5f,0.5f,0.5f,backgroundImage.color.a),
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
        RunManager.Instance.StartRun(character.ToString(), 50, new List<Relic>() {relic});
    }

    public string CharacterDescription(SelectableCharacter character)
    {
        string result = character switch
        {
            SelectableCharacter.EP => "L'EP est un vaisseau puissant avec une conservation énergétique optimale.",
            _ => "Description inconnue."
        };
        return result;
    }
    public string CharacterName(SelectableCharacter character)
    {
        string result = character switch
        {
            SelectableCharacter.EP => "Espadon de Prime",
            SelectableCharacter.MECA => "MECA",
            SelectableCharacter.CFI => "Croiseur de la Fédération Interstellaire",
            SelectableCharacter.GM => "Galactus Type-M",
            SelectableCharacter.ITI => "Imitateur Transcendant d'Intelligence",
            SelectableCharacter.GC => "GC",
            SelectableCharacter.ARCHI => "ARCHI",
            SelectableCharacter.PERF => "PERF",
            SelectableCharacter.MRIE => "MRIE",
            _ => "Unknown"
        };
        return $"<color=yellow>{result}</color>\n";
    }
}