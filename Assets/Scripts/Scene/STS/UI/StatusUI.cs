using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class StatusUI : MonoBehaviour
{
    public TextMeshProUGUI valueText;
    public Image icon;
    public Transform tooltipPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    private bool tooltipVisible = false;
    public void ToggleTooltip()
    {
        tooltipVisible = !tooltipVisible;
        tooltipPanel.gameObject.SetActive(tooltipVisible);
    }
    public void SetStatus(StatusEffect status)
    {
        icon.sprite = Resources.Load<Sprite>($"STS/Icons/{status.Name}");
        nameText.text = status.Name;
        valueText.text = $"{(status.Duration>0? status.Duration : status.Value)}";
        descriptionText.text = status.Describe();
    }
}