using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
public class PanelOptionView : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI label;
    public Button button;

    public void Init(PanelOption option)
    {
        label.text = option.text;
        label.text+="\n<color=grey><size=28>";
        foreach(var entry in option.entries)
        {
            label.text += EventOptionDescription.GetDescription(entry) + ".\n";
        }
        label.text += "</size></color>";
        icon.sprite = option.icon;
        if (icon.sprite==null)
        {
            icon.gameObject.SetActive(false);
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {option.action?.Invoke(); });
        if (option.action == null)
        {
            Debug.LogWarning("PanelOptionView initialized with null action for option: " + option.text);
        }

        UILayoutHelper.ApplyPreferredSizeAfterFrame(this, transform as RectTransform, fitWidth: true, fitHeight: true, extraWidth: 24f, extraHeight: 16f);
        StartCoroutine(ValidateLayoutConstraintsNextFrame());
    }

    private IEnumerator ValidateLayoutConstraintsNextFrame()
    {
        yield return null;

        GridLayoutGroup grid = GetComponentInParent<GridLayoutGroup>(true);
        if (grid != null)
        {
            Debug.LogWarning("PanelOptionView: parent GridLayoutGroup found. Grid cell size overrides preferred width/height and can force square options.", grid);
        }

        AspectRatioFitter fitter = GetComponentInParent<AspectRatioFitter>(true);
        if (fitter != null)
        {
            Debug.LogWarning("PanelOptionView: parent AspectRatioFitter found. Aspect ratio constraints can force square options.", fitter);
        }
    }
}