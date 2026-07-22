using UnityEngine;
using UnityEngine.UI;
using System;
public class NodeView : MonoBehaviour
{
    public MapNode node;
    public Button button;
    public Image icon;
    public Image outline;
    public CanvasGroup canvasGroup;

    private Vector3 baseScale = Vector3.one;

    public void Init(MapNode node, MapManager manager, float scaleMultiplier, Action onClick = null)
    {
        this.node = node;

        if (manager == null)
        {
            Debug.LogError($"NodeView '{name}' Init called with null MapManager.", this);
            return;
        }

        if (button == null)
        {
            button = GetComponent<Button>() ?? GetComponentInChildren<Button>();
        }

        if (button == null)
        {
            Debug.LogError($"No Button found for node view '{name}'.", this);
            return;
        }

        if (icon == null)
        {
            icon = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (icon == null)
        {
            Debug.LogWarning($"No Image found for node view '{name}'. Icon will not be set.", this);
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            manager.MoveToNode(node);
            onClick?.Invoke();
        });
        SetIcon(scaleMultiplier);
        baseScale = transform.localScale*UIAdaptiveScale.GetScreenScale();
        if (icon.sprite == null)
        {
            Debug.LogWarning($"Icon sprite not set for node view '{name}'.", this);
        }
        outline.enabled=node.visited;
    }

    void SetIcon(float scaleMultiplier)
    {
        var spritePath = $"STS/Map/NodeNeon{((int)node.type) + 1}";
        icon.sprite = Resources.Load<Sprite>(spritePath);
        transform.localScale = Vector3.one * scaleMultiplier;
        if (icon.sprite == null)
        {
            Debug.LogWarning($"Icon sprite not found for node type {node.type} at path '{spritePath}'.");
        }
        //icon.sprite=Resources.Load<Sprite>("STS/Map/" + node.type.ToString());
    }

    public void SetAccessibilityState(bool accessible, float pulse = 1f)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (accessible)
        {
            transform.localScale = baseScale * pulse;
            canvasGroup.alpha = Mathf.Lerp(0.82f, 1f, Mathf.InverseLerp(0.97f, 1.05f, pulse));
        }
        else
        {
            transform.localScale = baseScale;
            canvasGroup.alpha = 0.45f;
        }

        if (button != null)
            button.interactable = accessible;

        if (outline != null)
            outline.enabled = node != null && node.visited;
    }
}