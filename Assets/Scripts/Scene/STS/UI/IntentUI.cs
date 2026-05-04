using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class IntentUI : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Image icon;
    public void SetEffect(EffectEntry effect)
    {
        icon.sprite = Resources.Load<Sprite>($"STS/Icons/{effect.type}");
        valueText.text = effect.value > 0 ? $"{effect.value}" : "";
    }
}