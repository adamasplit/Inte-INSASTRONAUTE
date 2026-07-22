using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using System.Threading.Tasks;

public class MapManager : MonoBehaviour
{
    public MapNode currentNode;
    public MapView view;
    public MapGenerator generator = new();
    public Image blackOverlay;
    public Image mapBackgroundImage;

    private System.Collections.Generic.List<MapNode> allNodes;

    void Awake()
    {
        if (RunManager.Instance!=null)
        {
            RunManager.Instance.inCombat=false;
        }
        int act = Mathf.Min(RunManager.Instance != null ? RunManager.Instance.act+1 : 1,4);
        mapBackgroundImage.sprite = Resources.Load<Sprite>($"STS/Backgrounds/Map_BG{act}");
        StartCoroutine(InitializeMap());
    }

    private System.Collections.IEnumerator InitializeMap()
    {
        RunManager.Instance.eliteEncounter=false;
        RunManager.Instance.bossEncounter=false;
        StartCoroutine(FadeFromBlack(0.5f));
        yield return null;

        if (RunManager.Instance!=null && RunManager.Instance.map != null&&!RunManager.Instance.RegenerateMap)
        {
            allNodes = RunManager.Instance.map;
            currentNode = RunManager.Instance.currentNode;
            view.GenerateView(allNodes);
            StartCoroutine(NotifyReadyNextFrame());
            yield break;
        }
        var map = generator.Generate(RunManager.Instance != null&&RunManager.Instance.RegenerateMap ? RunManager.Instance.currentFloor : 0);
        if (RunManager.Instance!=null)
        {
            RunManager.Instance.currentNode = generator.startNode;
            RunManager.Instance.RegenerateMap = false;
            RunManager.Instance.map = map;
            RunManager.Instance.player.currentHP = RunManager.Instance.player.maxHP;
            RunManager.Instance.currentFloor=1;
        }
        allNodes = map;
        currentNode = generator.startNode;

        view.GenerateView(allNodes);
        StartCoroutine(NotifyReadyNextFrame());
    }

    private System.Collections.IEnumerator NotifyReadyNextFrame()
    {
        // Wait a couple frames to ensure all spawned UI elements complete their Awake/Start/OnEnable
        yield return null;
        yield return null;
        if (RunManager.Instance != null)
        {
            STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "map_ready");
        }
        STSSceneLoader.Instance?.SceneReady();
        RunManager.Instance?.SaveRunState();
    }

    public void MoveToNode(MapNode node)
    {
        if (currentNode == null)
        {
            Debug.LogError("currentNode is null");
            return;
        }

        if (!currentNode.next.Contains(node))
        {
            string allowed = currentNode.next.Count == 0
                ? "none"
                : string.Join(", ", currentNode.next.Select(n => n.id.ToString()));

            Debug.LogWarning(
                $"Blocked: {node.id}, allowed: {allowed}");
            return;
        }

        MapNode sourceNode = currentNode;
        currentNode = node;
        RunManager.Instance.currentNode = currentNode;
        node.visited = true;

        StartCoroutine(ResolveNode(sourceNode, node));

        view.RefreshView();
    }

    IEnumerator ResolveNode(MapNode sourceNode, MapNode node)
    {
        string sceneName="STS_Map";
        switch (node.type)
        {
            case NodeType.Combat:
                sceneName = "STS_Combat";
                break;

            case NodeType.Rest:
                RunManager.Instance.restCharges = RunManager.Instance.maxRestCharges;
                sceneName = "STS_Rest";
                break;

            case NodeType.Event:
                // In API-backed runs, authoritative routing comes from enter-node response.
                // Keep local fallback routing as event when no backend run is active.
                sceneName = "STS_Event";
                break;
            case NodeType.Elite:
                RunManager.Instance.eliteEncounter = true;
                sceneName = "STS_Combat";
                break;
            case NodeType.Boss:
                RunManager.Instance.bossEncounter = true;
                sceneName = "STS_Combat";
                break;
            default:
                Debug.LogError($"Unknown node type: {node.type}");
                break;
        }
        if (sceneName == "STS_Combat")
        {
            SFXManager.Instance.PlaySound("Encounter");
        }
        yield return StartCoroutine(FadeToBlack(0.5f));
        node.visited = true;
        RunManager.Instance.currentFloor+=1;
        STSRunAuditSystem.RecordNodeExited(RunManager.Instance, sourceNode, node, sceneName, "map_transition");

        if (RunManager.Instance != null && !string.IsNullOrWhiteSpace(RunManager.Instance.runId))
        {
            if (RunManager.Instance.unrestrictedMode)
            {
                STSSceneLoader.Instance.LoadScene(sceneName);
                yield break;
            }

            Debug.Log($"[STS-RUN] EnterNode request runId={RunManager.Instance.runId} nodeId={node.id} scene={sceneName} floor={RunManager.Instance.currentFloor}");
            Task<STSApiNodeEnterResponse> enterTask = STSApiClient.EnterNodeAsync(RunManager.Instance.runId, node.id);
            while (!enterTask.IsCompleted)
            {
                yield return null;
            }

            STSApiNodeEnterResponse enterResponse = null;
            if (enterTask.IsCompleted && !enterTask.IsFaulted && !enterTask.IsCanceled)
            {
                enterResponse = enterTask.Result;
                Debug.Log($"[STS-RUN] EnterNode response runId={RunManager.Instance.runId} nodeId={node.id} accepted={enterResponse != null && enterResponse.accepted} encounterId={enterResponse?.activeEncounter?.encounterInstanceId}");
                RunManager.Instance.ApplyNodeEnterResponse(enterResponse);

                if (enterResponse != null && enterResponse.accepted)
                {
                    bool hasEncounterPayload = enterResponse.activeEncounter != null
                        && enterResponse.activeEncounter.enemyIds != null
                        && enterResponse.activeEncounter.enemyIds.Count > 0;
                    if (hasEncounterPayload)
                    {
                        sceneName = "STS_Combat";
                    }
                    else if (!string.IsNullOrWhiteSpace(enterResponse.nodeType)
                             && enterResponse.nodeType.Equals("Event", System.StringComparison.OrdinalIgnoreCase))
                    {
                        sceneName = "STS_Event";
                    }
                    else if (!string.IsNullOrWhiteSpace(enterResponse.nodeType)
                             && enterResponse.nodeType.Equals("Rest", System.StringComparison.OrdinalIgnoreCase))
                    {
                        sceneName = "STS_Rest";
                    }
                }
            }
            else if (enterTask.IsFaulted)
            {
                Debug.LogWarning($"Node enter request failed for node {node.id}: {enterTask.Exception?.GetBaseException().Message}");
            }

            bool accepted = enterResponse != null && enterResponse.accepted;
            bool hasEncounter = enterResponse != null && enterResponse.activeEncounter != null && enterResponse.activeEncounter.enemyIds != null && enterResponse.activeEncounter.enemyIds.Count > 0;

            bool mustBlock = !accepted || (sceneName == "STS_Combat" && !hasEncounter);
            if (mustBlock)
            {
                string failureReason = !accepted
                    ? $"node enter rejected for node {node.id}"
                    : $"node enter missing encounter payload for node {node.id}";
                Debug.LogWarning($"{failureReason}. scene={sceneName} localNodeType={node.type} serverNodeType={enterResponse?.nodeType} runId={RunManager.Instance.runId}. Switching to unrestricted mode and continuing locally.");
                RunManager.Instance.EnableUnrestrictedMode(failureReason);
            }
        }

        STSSceneLoader.Instance.LoadScene(sceneName);
    }

    IEnumerator FadeToBlack(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            blackOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        blackOverlay.color = Color.black;
    }
    IEnumerator FadeFromBlack(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            blackOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        blackOverlay.color = new Color(0f, 0f, 0f, 0f);
    }
}