using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class IntentUI : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Image icon;
    private UIManager uiManager;
    private bool tooltipVisible = false;
    private EffectEntry effect;
    public void SetEffect(EffectEntry effect, UIManager uiManager)
    {
        this.uiManager = uiManager;
        this.effect = effect;
        switch (effect.type)
        {
            case EffectType.Status:
            {
                StatusEffect status = StatusEffect.Factory(effect.statusType,effect.value, effect.duration);
                icon.sprite=status.buff?Resources.Load<Sprite>($"STS/Icons/Intent/Buff"):Resources.Load<Sprite>($"STS/Icons/Intent/Debuff");
                break;
            }
            case EffectType.Multihit:
            {
                icon.sprite = Resources.Load<Sprite>($"STS/Icons/Intent/Damage");
                valueText.text = effect.value > 0 ? $"{effect.value}x{effect.duration}" : "";
                break;
            }
            default:
            {
                icon.sprite = Resources.Load<Sprite>($"STS/Icons/Intent/{effect.type}");
                valueText.text = effect.value > 0 ? $"{effect.value}" : "";
                break;
            }
        }
    }
    public void SetValue(int val)
    {
        valueText.text = val > 0 ? $"{val}" : "";
    }
    public void SetText(string text)
    {
        valueText.text = text;
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
                    StatusEffect status = StatusEffect.Factory(effect.statusType,effect.value, effect.duration);
                    name = "Statut " + (status.buff ? "positif" : "négatif");
                    description = "L'ennemi va appliquer "+status.Name;
                    break;
                }
                case EffectType.Multihit:
                {
                    name = "Multi-coup";
                    description = $"L'ennemi va attaquer {effect.duration} fois pour {effect.value} dégâts.";
                    break;
                }
                case EffectType.Armor:
                {
                    name = "Armure";
                    description = $"L'ennemi va recevoir {effect.value} d'Armure.";
                    break;
                }
                case EffectType.Damage:
                {
                    name = "Dégâts";
                    description = $"L'ennemi va infliger {effect.value} dégâts.";
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
}