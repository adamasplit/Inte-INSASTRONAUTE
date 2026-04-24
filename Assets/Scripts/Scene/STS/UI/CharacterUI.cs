using TMPro;
using UnityEngine;

public class CharacterUI : MonoBehaviour
{
    public TextMeshProUGUI hpText;
    public Transform statusContainer;
    public GameObject statusUIPrefab;
    Character character;

    public void SetCharacter(Character c)
    {
        character = c;
        Refresh();
    }

    public void Refresh()
    {
        if (character == null) return;
        hpText.text = $"{character.currentHP}/{character.maxHP} (Armor: {character.armor})";
        foreach (Transform child in statusContainer)
            Destroy(child.gameObject);
        foreach (var status in character.statusEffects)
        {
            var statusUIObj = Instantiate(statusUIPrefab, statusContainer);
            var statusUI = statusUIObj.GetComponent<StatusUI>();
            statusUI.SetStatus(status);
        }
    }
}