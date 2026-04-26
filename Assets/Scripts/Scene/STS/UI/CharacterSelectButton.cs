using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CharacterSelectButton : MonoBehaviour
{
    public SelectableCharacter character;
    public TextMeshProUGUI nameText;
    public Image backgroundImage;
    public Button button;
    public void Init(SelectableCharacter character, System.Action<SelectableCharacter> onClick)
    {
        this.character = character;
        nameText.text = character.ToString();
        button.onClick.AddListener(() => onClick(character));
    }
    public void Switch(bool selected)
    {
        backgroundImage.color = selected ? Color.black : Color.white;
        nameText.color = selected ? Color.white : Color.black;
    }
}