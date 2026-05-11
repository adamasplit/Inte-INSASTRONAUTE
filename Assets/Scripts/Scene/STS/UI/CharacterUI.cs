using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CharacterUI : MonoBehaviour
{   [Header("Status")]
    public Transform statusContainer;
    public GameObject statusUIPrefab;
    [Header("HP")]
    public HealthBar hp;

    [Header("Armor")]
    public Image armorImage;
    public TextMeshProUGUI armorText;
    public Character character;
    [Header("Intent")]
    public TextMeshProUGUI intentText;
    public Transform intentContainer;
    public GameObject intentUIPrefab;

    public void SetCharacter(Character c)
    {
        character = c;
        hp=GetComponentInChildren<HealthBar>();
        Refresh();
    }

    public void Refresh()
    {
        if (character == null) return;
        hp.SetHealth(character.currentHP, character.maxHP);
        armorText.text = character.armor > 0 ? $"{character.armor}" : "";
        hp.fill.color=character.armor > 0 ? Color.blue : Color.red;
        armorImage.enabled = character.armor > 0;
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
        Debug.Log($"Refreshing intent for {enemy.name}");
        var next = enemy.PeekNextAction();

        if (next == null)
        {
            intentText.text = "";
            return;
        }
        foreach (Transform child in intentContainer)
            Destroy(child.gameObject);
        intentText.text = next.name; 
        foreach (EffectEntry effect in next.effects)
        {
            IntentUI effectUIObj = Instantiate(intentUIPrefab, intentContainer).GetComponent<IntentUI>();
                effectUIObj.SetEffect(effect);
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
                Debug.Log($"Calculated damage: {val}");
                effectUIObj.SetValue(val);
            }
        }
    }
}