using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
public class PanelOptionView : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI label;
    public Button button;
    public void Init(PanelOption option)
    {
        label.text = option.text;
        label.text+="\n<color=grey><size=20>";
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
    }
}