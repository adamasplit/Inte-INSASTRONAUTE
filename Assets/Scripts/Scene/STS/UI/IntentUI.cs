using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
public class IntentUI : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Image icon;
    private UIManager uiManager;
    private bool tooltipVisible = false;
    private EffectEntry effect;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine animationRoutine;
    private string currentSignature;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetEffect(EffectEntry effect, UIManager uiManager, string displayText = null, bool animate = true)
    {
        this.uiManager = uiManager;
        this.effect = effect;
        string previousSignature = currentSignature;
        switch (effect.type)
        {
            case EffectType.Status:
            {
                StatusEffect status = StatusEffect.Factory(effect.statusType,effect.value, effect.duration,effect.cardID,effect.index);
                icon.sprite=status.buff?Resources.Load<Sprite>($"STS/Icons/Intent/Buff"):Resources.Load<Sprite>($"STS/Icons/Intent/Debuff");
                valueText.text = displayText ?? "";
                break;
            }
            case EffectType.Multihit:
            {
                icon.sprite = Resources.Load<Sprite>($"STS/Icons/Intent/Damage");
                valueText.text = displayText ?? (effect.value > 0 ? $"{effect.value}x{effect.duration}" : "");
                break;
            }
            default:
            {
                icon.sprite = Resources.Load<Sprite>($"STS/Icons/Intent/{effect.type}");
                valueText.text = displayText ?? (effect.value > 0 ? $"{effect.value}" : "");
                break;
            }
        }

        currentSignature = BuildSignature(effect, valueText != null ? valueText.text : string.Empty);

        if (!animate)
        {
            SetVisual(1f, 1f);
            return;
        }

        if (string.IsNullOrEmpty(previousSignature))
        {
            PlayShowAnimation();
        }
        else if (previousSignature != currentSignature)
        {
            PlayChangeAnimation();
        }
    }
    public void SetValue(int val)
    {
        SetText(val > 0 ? $"{val}" : "");
    }
    public void SetText(string text)
    {
        string previousText = valueText != null ? valueText.text : string.Empty;
        valueText.text = text;

        if (!string.IsNullOrEmpty(currentSignature) && previousText != text)
            PlayChangeAnimation();
    }
    public void ToggleTooltip()
    {
        tooltipVisible = !tooltipVisible;
        if (tooltipVisible)
        {
            string name = "";
            string description = "";
            switch (effect.type)
            {
                case EffectType.Status:
                {
                    StatusEffect status = StatusEffect.Factory(effect.statusType,effect.value, effect.duration,effect.cardID,effect.index);
                    name = "Statut " + (status.buff ? "positif" : "négatif");
                    description = "L'ennemi va appliquer "+status.Name;
                    break;
                }
                case EffectType.Multihit:
                {
                    name = "Multi-coup";
                    description = $"L'ennemi va attaquer {effect.duration} fois.";
                    break;
                }
                case EffectType.Armor:
                {
                    name = "Armure";
                    description = $"L'ennemi va recevoir de l'Armure.";
                    break;
                }
                case EffectType.Damage:
                {
                    name = "Dégâts";
                    description = $"L'ennemi va infliger des dégâts.";
                    break;
                }
                case EffectType.Heal:
                {
                    name = "Soin";
                    description = $"L'ennemi va se soigner.";
                    break;
                }
                case EffectType.AddCardToDiscardPile:
                {
                    name = "Ajout de carte";
                    description = $"L'ennemi va ajouter une carte à votre défausse.";
                    break;
                }
                case EffectType.AddCardToDrawPile:
                {
                    name = "Ajout de carte";
                    description = $"L'ennemi va ajouter une carte à votre pioche.";
                    break;
                }
                default:
                {
                    name = "???";
                    description = "L'ennemi va agir...";
                    break;
                }
            }
            TooltipManager.Instance.ShowTooltip(name, description, transform.position);
        }
        else
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    public void PlayRemoveAnimationAndDestroy()
    {
        if (!gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateRemoveRoutine());
    }

    private string BuildSignature(EffectEntry effect, string displayText)
    {
        return $"{effect.type}|{effect.statusType}|{effect.value}|{effect.duration}|{effect.cardID}|{effect.index}|{displayText}";
    }

    private void PlayShowAnimation()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        SetVisual(0f, 0.88f);
        animationRoutine = StartCoroutine(AnimateVisualRoutine(0f, 1f, 0.88f, 1f, 0.16f));
    }

    private void PlayChangeAnimation()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateChangeRoutine());
    }

    private IEnumerator AnimateChangeRoutine()
    {
        yield return AnimateVisualRoutineInternal(1f, 1f, 1f, 1.08f, 0.08f);
        yield return AnimateVisualRoutineInternal(1f, 1f, 1.08f, 1f, 0.1f);
        animationRoutine = null;
    }

    private IEnumerator AnimateRemoveRoutine()
    {
        yield return AnimateVisualRoutineInternal(canvasGroup != null ? canvasGroup.alpha : 1f, 0f, 1f, 0.88f, 0.12f);
        animationRoutine = null;
        Destroy(gameObject);
    }

    private IEnumerator AnimateVisualRoutine(float startAlpha, float endAlpha, float startScale, float endScale, float duration)
    {
        yield return AnimateVisualRoutineInternal(startAlpha, endAlpha, startScale, endScale, duration);
        animationRoutine = null;
    }

    private IEnumerator AnimateVisualRoutineInternal(float startAlpha, float endAlpha, float startScale, float endScale, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetVisual(Mathf.Lerp(startAlpha, endAlpha, t), Mathf.Lerp(startScale, endScale, t));
            yield return null;
        }

        SetVisual(endAlpha, endScale);
    }

    private void SetVisual(float alpha, float scale)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (rectTransform != null)
            rectTransform.localScale = Vector3.one * scale;
    }
}