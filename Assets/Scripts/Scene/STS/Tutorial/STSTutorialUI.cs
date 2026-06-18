using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class STSTutorialUI : MonoBehaviour
{
    public static STSTutorialUI Instance;

    public TextMeshProUGUI tutorialText;
    public GameObject textBox;
    public GameObject tutorialRoot;
    public STSTutorialHighlight highlight;
    public GameObject overlay;
    public CanvasGroup overlayCanvasGroup;
    public bool isActive => tutorialRoot.activeSelf;

    RectTransform dummyMapRoot;
    readonly Dictionary<NodeType, RectTransform> dummyMapNodes = new();
    bool dummyMapBuilt;

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
    public void SetOverlayAlpha(float alpha)
    {
        overlayCanvasGroup.alpha = alpha;
    }

    public void ShowDummyMapPreview()
    {
        EnsureDummyMapPreview();

        if (dummyMapRoot != null)
        {
            dummyMapRoot.gameObject.SetActive(true);
        }
    }

    public void HideDummyMapPreview()
    {
        if (dummyMapRoot != null)
        {
            dummyMapRoot.gameObject.SetActive(false);
        }
    }

    public void HighlightDummyMapNode(NodeType type, Canvas canvas, float padding = 0f)
    {
        EnsureDummyMapPreview();

        if (dummyMapNodes.TryGetValue(type, out var node) && node != null)
        {
            Highlight(node, canvas, padding);
        }
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
        HideDummyMapPreview();
        Unhighlight();
        tutorialRoot.SetActive(false);
    }

    void EnsureDummyMapPreview()
    {
        if (dummyMapBuilt)
            return;

        dummyMapBuilt = true;

        if (tutorialRoot == null)
            return;

        var rootObject = new GameObject("DummyMapPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootObject.transform.SetParent(tutorialRoot.transform, false);

        dummyMapRoot = rootObject.GetComponent<RectTransform>();
        dummyMapRoot.anchorMin = new Vector2(0.5f, 0.5f);
        dummyMapRoot.anchorMax = new Vector2(0.5f, 0.5f);
        dummyMapRoot.pivot = new Vector2(0.5f, 0.5f);
        dummyMapRoot.sizeDelta = new Vector2(900f, 320f);
        dummyMapRoot.anchoredPosition = new Vector2(0f, 90f);

        var background = rootObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.07f, 0.11f, 0.92f);

        var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(dummyMapRoot, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(320f, 30f);

        var title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "Carte d'exemple";
        title.fontSize = 22f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.97f, 1f, 0.95f);

        var nodes = new (NodeType type, string label, Vector2 position)[]
        {
            (NodeType.Start, "Depart", new Vector2(-360f, -70f)),
            (NodeType.Combat, "Combat", new Vector2(-220f, 20f)),
            (NodeType.Elite, "Elite", new Vector2(-40f, -20f)),
            (NodeType.Rest, "Repos", new Vector2(120f, 70f)),
            (NodeType.Event, "Event", new Vector2(290f, -10f)),
            (NodeType.Boss, "Boss", new Vector2(430f, 80f))
        };

        for (int i = 0; i < nodes.Length - 1; i++)
        {
            CreateConnection(dummyMapRoot, nodes[i].position, nodes[i + 1].position);
        }

        foreach (var node in nodes)
        {
            dummyMapNodes[node.type] = CreateNode(dummyMapRoot, node.type, node.label, node.position);
        }
    }

    RectTransform CreateNode(RectTransform parent, NodeType type, string label, Vector2 position)
    {
        var nodeObject = new GameObject($"{type}Node", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        nodeObject.transform.SetParent(parent, false);

        var nodeRect = nodeObject.GetComponent<RectTransform>();
        nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
        nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRect.pivot = new Vector2(0.5f, 0.5f);
        nodeRect.sizeDelta = new Vector2(74f, 74f);
        nodeRect.anchoredPosition = position;

        var nodeImage = nodeObject.GetComponent<Image>();
        nodeImage.sprite = Resources.Load<Sprite>($"STS/Map/NodeNeon{((int)type) + 1}");
        nodeImage.color = Color.white;

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(nodeRect, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -6f);
        labelRect.sizeDelta = new Vector2(140f, 24f);

        var labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.9f, 0.95f, 1f, 0.95f);

        return nodeRect;
    }

    void CreateConnection(RectTransform parent, Vector2 start, Vector2 end)
    {
        var lineObject = new GameObject("Connection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(parent, false);

        var lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 delta = end - start;
        lineRect.sizeDelta = new Vector2(delta.magnitude, 8f);
        lineRect.anchoredPosition = (start + end) * 0.5f;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var lineImage = lineObject.GetComponent<Image>();
        lineImage.color = new Color(0.45f, 0.7f, 1f, 0.25f);
        lineObject.transform.SetAsFirstSibling();
    }

}