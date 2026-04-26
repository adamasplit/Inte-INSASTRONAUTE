using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CharacterUI : MonoBehaviour
{   [Header("Status")]
    public Transform statusContainer;
    public GameObject statusUIPrefab;
    [Header("HP")]
    public Image hpFill;
    public TextMeshProUGUI hpText;

    [Header("Armor")]
    public TextMeshProUGUI armorText;
    Character character;
    [Header("Intent")]
    public TextMeshProUGUI intentText;

    public void SetCharacter(Character c)
    {
        character = c;
        Refresh();
    }

    public void Refresh()
    {
        if (character == null) return;
        hpText.text = $"{character.currentHP}/{character.maxHP}";
        float hpRatio = (float)character.currentHP / character.maxHP;
        hpFill.fillAmount = hpRatio;
        armorText.text = character.armor > 0 ? $"Armor: {character.armor}" : "";
        foreach (Transform child in statusContainer)
            Destroy(child.gameObject);
        foreach (var status in character.statusEffects)
        {
            var statusUIObj = Instantiate(statusUIPrefab, statusContainer);
            var statusUI = statusUIObj.GetComponent<StatusUI>();
            statusUI.SetStatus(status);
        }
        if (!character.isPlayer)
        {
            // Refresh the enemy's intent
            RefreshIntent(character as Enemy);
        }
    }
    public void RefreshIntent(Enemy enemy)
    {
        var next = enemy.PeekNextAction();

        if (next == null)
        {
            intentText.text = "";
            return;
        }

        intentText.text = next.name; 
        foreach (EffectEntry effect in next.effects)
        {
            if (effect.type == EffectType.Damage)
            {
                CombatManager cm = FindObjectOfType<CombatManager>();
                int val = BattleCalculator.GetModifiedValue(effect.value, StatType.Damage, new EffectContext
                {
                    source = enemy,
                    target = RunManager.Instance.player,
                    combat = cm,
                    state = cm.state,
                    card = new CardInstance(next)
                });
                intentText.text += $"({val})";
            }
        }
    }
}