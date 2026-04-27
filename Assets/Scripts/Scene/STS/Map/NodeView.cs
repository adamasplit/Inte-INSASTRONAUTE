using UnityEngine;
using UnityEngine.UI;
using System;
public class NodeView : MonoBehaviour
{
    public MapNode node;
    public Button button;
    public Image icon;

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

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            manager.MoveToNode(node);
            onClick?.Invoke();
        });
        SetIcon();
    }

    void SetIcon()
    {
        icon.sprite=Resources.Load<Sprite>("STS/Map/" + node.type.ToString());
    }
}