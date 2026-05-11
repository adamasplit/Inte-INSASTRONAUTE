using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class IntentUI : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Image icon;
    public void SetEffect(EffectEntry effect)
    {
        if (effect.type == EffectType.Status)
        {
            StatusEffect status = StatusEffect.Factory(effect.statusType,effect.value, effect.duration);
            icon.sprite=status.buff?Resources.Load<Sprite>($"STS/Icons/Buff"):Resources.Load<Sprite>($"STS/Icons/Debuff");
        }
        else
        {
            icon.sprite = Resources.Load<Sprite>($"STS/Icons/{effect.type}");
            valueText.text = effect.value > 0 ? $"{effect.value}" : "";
        }
    }
    public void SetValue(int val)
    {
        valueText.text = val > 0 ? $"{val}" : "";
    }
}