using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading.Tasks;

public class PullToRefreshAsync : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public float pullThreshold = 80f;

    private ScrollRect scrollRect;
    private Vector2 startDragPosition;
    private bool isAtTop;
    private bool isRefreshing;

    // Référence vers ton script leaderboard
    public MonoBehaviour leaderboardScript;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    void Update()
    {
        isAtTop = scrollRect.verticalNormalizedPosition >= 0.99f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isAtTop || isRefreshing) return;
        startDragPosition = eventData.position;
    }

    public async void OnEndDrag(PointerEventData eventData)
    {
        if (!isAtTop || isRefreshing) return;

        float dragDistance = eventData.position.y - startDragPosition.y;

        if (dragDistance > pullThreshold)
        {
            await TriggerRefreshAsync();
        }
    }

    private async Task TriggerRefreshAsync()
    {
        isRefreshing = true;
        Debug.Log("Pull to refresh async");

        // Appel de ta fonction async
        var method = leaderboardScript.GetType().GetMethod("RefreshLeaderboardAsync");
        if (method != null)
        {
            Task task = (Task)method.Invoke(leaderboardScript, null);
            await task;
        }
        else
        {
            Debug.LogError("RefreshLeaderboard not found");
        }

        isRefreshing = false;
    }
}
