using TMPro;
using UnityEngine;

public class STSTutorialUI : MonoBehaviour
{
    public static STSTutorialUI Instance;

    public TextMeshProUGUI tutorialText;
    public GameObject textBox;
    public GameObject tutorialRoot;
    public STSTutorialHighlight highlight;
    public GameObject overlay;
    public bool isActive => tutorialRoot.activeSelf;
    void Awake()
    {
        Instance = this;
    }

    public void ShowText(string text)
    {
        tutorialRoot.SetActive(true);
        textBox.SetActive(true);
        tutorialText.text = text;
    }
    public void hideText()
    {
        textBox.SetActive(false);
    }
    public void ShowOverlay()
    {
        tutorialRoot.SetActive(true);
        overlay.SetActive(true);
    }
    public void HideOverlay()
    {
        overlay.SetActive(false);
    }
    public void Highlight(RectTransform target,Canvas canvas, float padding = 0f)
    {
        highlight.Highlight(HighlightTarget.FromRectTransform(target, canvas), padding);
    }
    public void Unhighlight()
    {
        highlight.Hide();
    }
    public void Hide()
    {
        tutorialRoot.SetActive(false);
    }

}