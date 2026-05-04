using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HealthBar : MonoBehaviour
{
    public Image fill;
    public TextMeshProUGUI text;

    public void SetHealth(int current, int max)
    {
        text.text = $"{current}/{max}";
        fill.fillAmount = (float)current / max;
    }
}