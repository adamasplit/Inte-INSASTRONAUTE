using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Tooltip : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    private TooltipManager tooltipManager;
    public float padding = 20f;
    public float maxWidth = 300f;
    public void SetTooltip(TooltipManager tooltipManager, string title, string description)
    {
        this.tooltipManager = tooltipManager;
        titleText.text = title;
        descriptionText.text = description;
        float naturalWidth = Mathf.Max(titleText.preferredWidth, descriptionText.preferredWidth);
        float tooltipWidth = Mathf.Min(naturalWidth + padding * 2f, maxWidth);
        descriptionText.GetComponent<LayoutElement>().preferredWidth = tooltipWidth - padding * 2f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
    public void Hide()
    {
        if (tooltipManager != null)
            tooltipManager.HideTooltip();
        else
            Destroy(gameObject);
    }
}