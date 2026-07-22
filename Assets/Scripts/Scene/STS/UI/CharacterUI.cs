using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public UIManager uiManager;
    CanvasGroup canvasGroup;
    bool deathAnimationPlayed;
    int lastArmorValue = -1;
    Coroutine armorAnimation;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void SetCharacter(Character c, UIManager uiManager)
    {
        this.uiManager = uiManager;
        character = c;
        hp=GetComponentInChildren<HealthBar>();
        Refresh();
    }

    public void Refresh()
    {
        if (character == null) return;
        hp.SetHealth(character.currentHP, character.maxHP);
        hp.fillImage.color=character.armor > 0 ? Color.blue : Color.red;
        foreach (var status in character.statusEffects.ToList())
        {
            status.Update(character);
        }
        character.ExpireStatuses();
        int currentArmor = character.armor;
        armorText.text = currentArmor > 0 ? $"{currentArmor}" : "";
        UpdateArmorImage(currentArmor);
        lastArmorValue = currentArmor;

        Dictionary<StatusEffect, StatusUI> activeStatusUIs = new Dictionary<StatusEffect, StatusUI>();
        foreach (Transform child in statusContainer)
        {
            StatusUI statusUI = child.GetComponent<StatusUI>();
            if (statusUI != null && statusUI.BoundStatus != null)
            {
                activeStatusUIs[statusUI.BoundStatus] = statusUI;
            }
            else
            {
                Destroy(child.gameObject);
            }
        }

        

        HashSet<StatusEffect> visibleStatuses = new HashSet<StatusEffect>();
        for (int i = 0; i < character.statusEffects.Count; i++)
        {
            var status = character.statusEffects[i];
            visibleStatuses.Add(status);

            if (activeStatusUIs.TryGetValue(status, out var statusUI))
            {
                statusUI.SetStatus(status, uiManager, character.isPlayer,false);
                statusUI.transform.SetSiblingIndex(i);
            }
            else
            {
                var statusUIObj = Instantiate(statusUIPrefab, statusContainer);
                statusUI = statusUIObj.GetComponent<StatusUI>();
                statusUI.SetStatus(status, uiManager, character.isPlayer);
                statusUI.transform.SetSiblingIndex(i);
            }
        }

        foreach (var kvp in activeStatusUIs)
        {
            if (!visibleStatuses.Contains(kvp.Key))
            {
                kvp.Value.PlayRemoveAnimationAndDestroy();
            }
        }
        if (!character.isPlayer)
        {
            // Refresh the enemy's intent
            RefreshIntent(character as Enemy);
        }
    }

    void UpdateArmorImage(int currentArmor)
    {
        if (armorImage == null)
        {
            return;
        }

        bool hadArmor = lastArmorValue > 0;
        bool hasArmor = currentArmor > 0;

        if (!hadArmor && !hasArmor)
        {
            armorImage.enabled = false;
            SetArmorVisual(0f, 0.85f);
            return;
        }

        if (armorAnimation != null)
        {
            StopCoroutine(armorAnimation);
            armorAnimation = null;
        }

        if (!hadArmor && hasArmor)
        {
            armorImage.enabled = true;
            armorAnimation = StartCoroutine(AnimateArmorImage(0f, 1f, 0.85f, 1f, false));
            return;
        }

        if (hadArmor && !hasArmor)
        {
            armorAnimation = StartCoroutine(AnimateArmorImage(1f, 0f, 1f, 0.85f, true));
            return;
        }

        armorImage.enabled = true;
        SetArmorVisual(1f, 1f);
    }

    IEnumerator AnimateArmorImage(float startAlpha, float endAlpha, float startScale, float endScale, bool disableOnComplete)
    {
        RectTransform armorRect = armorImage.rectTransform;
        Color originalColor = armorImage.color;
        float elapsed = 0f;
        const float duration = 0.16f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetArmorVisual(Mathf.Lerp(startAlpha, endAlpha, t), Mathf.Lerp(startScale, endScale, t));
            yield return null;
        }

        SetArmorVisual(endAlpha, endScale);
        armorImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, armorImage.color.a);
        if (disableOnComplete && endAlpha <= 0f)
        {
            armorImage.enabled = false;
        }
        armorAnimation = null;
    }

    void SetArmorVisual(float alpha, float scale)
    {
        if (armorImage == null)
        {
            return;
        }

        RectTransform armorRect = armorImage.rectTransform;
        Color color = armorImage.color;
        color.a = alpha;
        armorImage.color = color;
        armorRect.localScale = Vector3.one * scale;
    }
    public void RefreshIntent(Enemy enemy)
    {
        var next = enemy.PeekNextAction();

        if (next == null)
        {
            intentText.text = "";
            foreach (Transform child in intentContainer)
            {
                IntentUI intentUI = child.GetComponent<IntentUI>();
                if (intentUI != null)
                    intentUI.PlayRemoveAnimationAndDestroy();
                else
                    Destroy(child.gameObject);
            }
            return;
        }

        List<IntentUI> activeIntentUIs = new List<IntentUI>();
        foreach (Transform child in intentContainer)
        {
            IntentUI intentUI = child.GetComponent<IntentUI>();
            if (intentUI != null)
                activeIntentUIs.Add(intentUI);
            else
                Destroy(child.gameObject);
        }

        intentText.text = next.name; 
        for (int i = 0; i < next.effects.Count; i++)
        {
            EffectEntry effect = next.effects[i];
            string displayText = GetIntentDisplayText(enemy, next, effect);

            IntentUI effectUIObj;
            if (i < activeIntentUIs.Count && activeIntentUIs[i] != null)
            {
                effectUIObj = activeIntentUIs[i];
            }
            else
            {
                effectUIObj = Instantiate(intentUIPrefab, intentContainer).GetComponent<IntentUI>();
            }

            effectUIObj.transform.SetSiblingIndex(i);
            effectUIObj.SetEffect(effect, uiManager, displayText, true);
        }

        for (int i = next.effects.Count; i < activeIntentUIs.Count; i++)
        {
            if (activeIntentUIs[i] != null)
                activeIntentUIs[i].PlayRemoveAnimationAndDestroy();
        }
    }

    private string GetIntentDisplayText(Enemy enemy, STSCardData next, EffectEntry effect)
    {
        if (effect.type!=EffectType.Damage && effect.type!=EffectType.Armor && effect.type!=EffectType.Multihit)
        {
            return "";
        }
        if (effect.type == EffectType.Damage)
        {
            CombatManager cm = FindObjectOfType<CombatManager>();
            int val = BattleCalculator.GetModifiedValue(effect.value, StatType.Damage, new EffectContext
            {
                source = enemy,
                target = RunManager.Instance.player,
                combat = cm,
                state = cm.state,
                card = new CardInstance(next),
                isPreview=true
            });
            return val > 0 ? $"{val}" : "";
        }
        else if (effect.type==EffectType.Armor)
        {
            CombatManager cm = FindObjectOfType<CombatManager>();
            int val = BattleCalculator.GetModifiedValue(effect.value, StatType.Armor, new EffectContext
            {
                source = enemy,
                target = RunManager.Instance.player,
                combat = cm,
                state = cm.state,
                card = new CardInstance(next)
            });
            return val > 0 ? $"{val}" : "";
        }
        else if (effect.type == EffectType.Multihit)
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
            return $"{val}x{effect.duration}";
        }

        return effect.value > 0 ? $"{effect.value}" : "";
    }

    public IEnumerator PlayDeathAnimation(float duration = 0.65f)
    {
        if (deathAnimationPlayed)
            yield break;

        deathAnimationPlayed = true;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        RectTransform rect = transform as RectTransform;
        Vector3 startScale = rect.localScale;
        Vector2 startPosition = rect.anchoredPosition;
        Quaternion startRotation = rect.localRotation;
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        Color[] originalColors = new Color[graphics.Length];

        for (int i = 0; i < graphics.Length; i++)
        {
            originalColors[i] = graphics[i].color;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float flashDuration = Mathf.Min(0.18f, duration * 0.35f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fadeT = Mathf.SmoothStep(0f, 1f, t);
            float flashT = flashDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / flashDuration);
            float flash = flashT < 0.5f ? flashT * 2f : (1f - flashT) * 2f;

            rect.localScale = Vector3.Lerp(startScale, startScale * 0.12f, t);
            rect.anchoredPosition = startPosition + new Vector2(0f, -24f * fadeT);
            rect.localRotation = startRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 8f, t));
            canvasGroup.alpha = 1f - fadeT;

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null)
                    continue;

                Color tinted = Color.Lerp(originalColors[i], Color.white, flash * 0.75f);
                tinted.a = originalColors[i].a * (1f - fadeT);
                graphics[i].color = tinted;
            }

            yield return null;
        }

        rect.localScale = startScale * 0.12f;
        rect.anchoredPosition = startPosition + new Vector2(0f, -24f);
        rect.localRotation = startRotation;
        canvasGroup.alpha = 0f;
    }
}