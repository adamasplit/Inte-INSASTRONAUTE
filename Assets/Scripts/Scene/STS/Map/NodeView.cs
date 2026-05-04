using UnityEngine;
using UnityEngine.UI;
using System;
public class NodeView : MonoBehaviour
{
    public MapNode node;
    public Button button;
    public Image icon;
    public Image outline;

    public void Init(MapNode node, MapManager manager, Action onClick = null)
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
        SetIcon();
        if (icon.sprite == null)
        {
            Debug.LogWarning($"Icon sprite not set for node view '{name}'.", this);
        }
        outline.enabled=node.visited;
    }

    void SetIcon()
    {
        var spritePath = $"STS/Map/Node{((int)node.type) + 1}";
        icon.sprite = Resources.Load<Sprite>(spritePath);
        if (icon.sprite == null)
        {
            Debug.LogWarning($"Icon sprite not found for node type {node.type} at path '{spritePath}'.");
        }
        //icon.sprite=Resources.Load<Sprite>("STS/Map/" + node.type.ToString());
    }
}