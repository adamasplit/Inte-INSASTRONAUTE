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
        icon.sprite = option.icon;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {option.action?.Invoke(); });
        if (option.action == null)
        {
            Debug.LogWarning("PanelOptionView initialized with null action for option: " + option.text);
        }
    }
}