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
        int hp = character switch
        {
            SelectableCharacter.EP => 80,
            SelectableCharacter.MECA => 70,
            SelectableCharacter.CFI => 90,
            SelectableCharacter.GM => 75,
            SelectableCharacter.ITI => 85,
            SelectableCharacter.GC => 65,
            SelectableCharacter.ARCHI => 100,
            SelectableCharacter.PERF => 60,
            SelectableCharacter.MRIE => 95,
            _ => 80
        };
        RunManager.Instance.StartRun(character.ToString(), hp, new List<Relic>() {relic});
    }

    public string CharacterDescription(SelectableCharacter character)
    {
        string result = character switch
        {
            SelectableCharacter.EP => "L'EP est un vaisseau puissant avec une conservation énergétique optimale.",
            SelectableCharacter.MECA => "Une machine conçue pour l'exploration, avec une durabilité et une résilience hors du commun.",
            SelectableCharacter.GM=> "Un influent politique qui sait user de sa position.",
            SelectableCharacter.CFI => "Un chercheur dédié à l'exploration et à la découverte.",
            SelectableCharacter.MRIE => "Une force de la nature destructrice, déclenchant des phénomènes cosmiques sur son passage.",
            SelectableCharacter.ITI => "Une entité capable de s'adapter et d'évoluer en absorbant les pouvoirs de ses adversaires.",
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
            SelectableCharacter.CFI => "Chercheur de la Fédération Interstellaire",
            SelectableCharacter.GM => "Grand Magistrat",
            SelectableCharacter.ITI => "Imitatrice Transcendante d'Intelligence",
            SelectableCharacter.GC => "Gardienne du Cosmos",
            SelectableCharacter.ARCHI => "ARCHI",
            SelectableCharacter.PERF => "PERF",
            SelectableCharacter.MRIE => "Messager du Rêve et de l'Insondable Éternel",
            _ => "Unknown"
        };
        return $"<color=yellow>{result}</color>\n";
    }
}