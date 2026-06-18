using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MapView : MonoBehaviour
{
    public ScrollRect scrollRect; // Assign in inspector
    public MapManager mapManager;
    public GameObject nodePrefab;
    public Transform mapPanel;
    public Transform linesContainer;
    public Camera uiCamera;
    public Material lineMaterial;

    public float spacingX = 200f;
    public float spacingY = 250f;

    private Dictionary<MapNode, RectTransform> nodeToUI = new();
    private readonly List<LineEntry> lineEntries = new();

    private class LineEntry
    {
        public MapNode source;
        public MapNode target;
        public LineRenderer renderer;
    }

    public void GenerateView(List<MapNode> allNodes)
    {
        // clear UI
        foreach (Transform child in mapPanel)
            Destroy(child.gameObject);

        foreach (Transform child in linesContainer)
            Destroy(child.gameObject);

        nodeToUI.Clear();
        lineEntries.Clear();
        int maxFloor = 0;
        // 1. Spawn nodes
        foreach (var node in allNodes)
        {
            GameObject obj = Instantiate(nodePrefab, mapPanel);
            RectTransform rt = obj.GetComponent<RectTransform>();

            // Use posX for natural horizontal placement (0..1 mapped to panel width)
            float panelWidth = mapPanel.parent.GetComponent<RectTransform>().rect.width;
            float xPos = (node.posX - 0.5f) * (panelWidth - spacingX); // center at 0, spread out
            rt.anchoredPosition = new Vector2(
                xPos,
                node.floor * spacingY
            );
            if (node.type==NodeType.Start)
            {
                rt.anchoredPosition = new Vector2(0f, 0f);
            }
            if (node.floor > maxFloor) maxFloor = node.floor;

            nodeToUI[node] = rt;

            NodeView nodeView = obj.GetComponent<NodeView>() 
                                ?? obj.GetComponentInChildren<NodeView>();

            nodeView.Init(node, mapManager, RefreshView);
        }
        mapPanel.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(mapPanel.parent.GetComponent<RectTransform>().sizeDelta.x, (maxFloor + 1) * spacingY + 400f);
        // 2. Draw connections
        DrawConnections(allNodes);

        RefreshView();

        // Scroll to current node if possible
        if (mapManager != null && mapManager.currentNode != null && scrollRect != null && nodeToUI.ContainsKey(mapManager.currentNode))
        {
            RectTransform nodeRT = nodeToUI[mapManager.currentNode];
            // Convert node position to viewport space
            Vector2 nodePos = nodeRT.anchoredPosition;
            float contentHeight = ((RectTransform)mapPanel).rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            // Calculate normalized scroll position (0 = bottom, 1 = top)
            float targetY = nodePos.y;
            float normalized = Mathf.Clamp01((targetY - viewportHeight / 2) / (contentHeight - viewportHeight));
            // In Unity, verticalNormalizedPosition: 1 = top, 0 = bottom
            scrollRect.verticalNormalizedPosition = 1f - normalized;
        }
    }
    void DrawConnections(List<MapNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!nodeToUI.ContainsKey(node)) continue;

            foreach (var next in node.next)
            {
                if (!nodeToUI.ContainsKey(next)) continue;

                CreateLine(node, next, nodeToUI[node], nodeToUI[next]);
            }
        }
    }
    void CreateLine(MapNode source, MapNode target, RectTransform a, RectTransform b)
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

        lineEntries.Add(new LineEntry
        {
            source = source,
            target = target,
            renderer = lr
        });
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
                : new Color(1f, 1f, 1f, 1f);
        }

        RefreshLines();
    }

    void RefreshLines()
    {
        if (mapManager == null || mapManager.currentNode == null)
            return;

        foreach (var entry in lineEntries)
        {
            // Always show all lines
            entry.renderer.gameObject.SetActive(true);
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
        float pulse = Mathf.Lerp(0.5f, 3f, Mathf.PingPong(Time.time, 1f));

        foreach (var entry in lineEntries)
        {
            var lr = entry.renderer;
            if (lr == null || !lr.gameObject.activeSelf) continue;

            bool isCrossableNow = mapManager != null && mapManager.currentNode != null && entry.source == mapManager.currentNode;
            bool isAlreadyCrossed = entry.source.visited && entry.target.visited;
            if (isCrossableNow || isAlreadyCrossed)
            {
                Color baseColor = new Color(0.2f, 0.6f, 1f);
                lr.startColor = baseColor * pulse;
                lr.endColor = baseColor * (pulse * 0.5f);
            }
            else
            {
                // Dim white for inaccessible lines
                Color dimWhite = new Color(1f, 1f, 1f, 0.2f);
                lr.startColor = dimWhite;
                lr.endColor = dimWhite;
            }
        }
    }
}