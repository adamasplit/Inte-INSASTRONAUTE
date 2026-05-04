using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MapView : MonoBehaviour
{
    public MapManager mapManager;
    public GameObject nodePrefab;
    public Transform mapPanel;
    public Transform linesContainer;
    public Camera uiCamera;
    public Material lineMaterial;

    public float spacingX = 200f;
    public float spacingY = 250f;

    private Dictionary<MapNode, RectTransform> nodeToUI = new();

    public void GenerateView(List<MapNode> allNodes)
    {
        // clear UI
        foreach (Transform child in mapPanel)
            Destroy(child.gameObject);

        nodeToUI.Clear();
        int maxFloor = 0;
        // 1. Spawn nodes
        foreach (var node in allNodes)
        {
            GameObject obj = Instantiate(nodePrefab, mapPanel);
            RectTransform rt = obj.GetComponent<RectTransform>();

            // POSITION (basée sur graph, pas grille)
            rt.anchoredPosition = new Vector2(
                (node.x - 2) * spacingX,
                node.floor * spacingY
            );
            if (node.floor > maxFloor) maxFloor = node.floor;

            nodeToUI[node] = rt;

            NodeView nodeView = obj.GetComponent<NodeView>() 
                                ?? obj.GetComponentInChildren<NodeView>();

            nodeView.Init(node, mapManager, RefreshView);
        }
        mapPanel.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(mapPanel.parent.GetComponent<RectTransform>().sizeDelta.x, (maxFloor + 1) * spacingY + 100f);
        // 2. Draw connections
        DrawConnections(allNodes);

        RefreshView();
    }
    void DrawConnections(List<MapNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!nodeToUI.ContainsKey(node)) continue;

            foreach (var next in node.next)
            {
                if (!nodeToUI.ContainsKey(next)) continue;

                CreateLine(nodeToUI[node], nodeToUI[next]);
            }
        }
    }
    void CreateLine(RectTransform a, RectTransform b)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(linesContainer, false);

        LineRenderer lr = line.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.useWorldSpace = false;

        Vector3 start = UIToLocal(a);
        Vector3 end = UIToLocal(b);

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        // MATERIAL
        lr.material = lineMaterial;

        // WIDTH (taper)
        lr.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.2f)
        );
        lr.widthMultiplier = 0.1f;

        // COLOR + GLOW (HDR)
        Color glow = new Color(0.2f, 0.6f, 1f) * 3f;

        lr.startColor = glow;
        lr.endColor = glow * 0.5f;
        lr.sortingLayerName = "UI";
    }
    public void RefreshView()
    {
        if (mapManager == null || mapManager.currentNode == null)
            return;

        foreach (var kvp in nodeToUI)
        {
            var node = kvp.Key;
            var rt = kvp.Value;

            var nodeView = rt.GetComponent<NodeView>();

            bool isReachable = mapManager.currentNode.next.Contains(node);

            nodeView.button.interactable = isReachable;

            nodeView.icon.color = isReachable
                ? Color.white
                : new Color(1f, 1f, 1f, 0.4f);
        }
    }

    Vector3 UIToLocal(RectTransform rt)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            linesContainer as RectTransform,
            RectTransformUtility.WorldToScreenPoint(uiCamera, rt.position),
            uiCamera,
            out localPos
        );

        return localPos;
    }

    void Update()
    {
        float pulse = Mathf.Lerp(1f, 2f, Mathf.PingPong(Time.time, 1f));

        foreach (Transform child in linesContainer)
        {
            var lr = child.GetComponent<LineRenderer>();
            if (lr == null) continue;

            Color baseColor = new Color(0.2f, 0.6f, 1f);
            lr.startColor = baseColor * pulse;
            lr.endColor = baseColor * (pulse * 0.5f);
        }
    }
}