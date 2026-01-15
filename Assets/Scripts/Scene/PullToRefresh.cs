using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading.Tasks;

public class PullToRefresh : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float pullThreshold = 40f; // Distance in pixels to trigger refresh

    [Header("UI")]
    public Image refreshIcon;

    [Header("Leaderboard")]
    public MonoBehaviour leaderboardScript;

    private ScrollRect scrollRect;
    private Vector2 startDragPosition;
    bool IsAtTop()
    {
            return content.anchoredPosition.y <= pullThreshold;

    }
    bool IsAtTopOfLeaderboard()
{
    return scrollRect.verticalNormalizedPosition >= 0.99f; // Use a threshold for floating point precision
}
    private bool isRefreshing;
    private RectTransform content;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        refreshIcon.canvasRenderer.SetAlpha(0);
        content= scrollRect.content;
    }

    void Update()
    {


        if (isRefreshing)
        {
            refreshIcon.transform.Rotate(0, 0, -360f * Time.deltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsAtTop() || isRefreshing) return;
        startDragPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isRefreshing) return;
        float dragDistance = 1f - content.anchoredPosition.y;
        float progress = (dragDistance / pullThreshold)/2f;

        refreshIcon.canvasRenderer.SetAlpha(Mathf.Min(progress, 1f));
        refreshIcon.transform.rotation = Quaternion.Euler(0, 0, -180f * Mathf.Lerp(progress, 1f, 0.5f));
    }

    public async void OnEndDrag(PointerEventData eventData)
    {
        if (isRefreshing) return;

        float dragDistance = content.anchoredPosition.y;

        if (IsAtTop()&&(IsAtTopOfLeaderboard()))
        {
            await TriggerRefreshAsync();
        }
        else
        {
            ResetIcon();
        }
    }

    private async Task TriggerRefreshAsync()
    {
        isRefreshing = true;

        refreshIcon.canvasRenderer.SetAlpha(1f);
        refreshIcon.transform.rotation = Quaternion.identity;


        Debug.Log("[PullToRefresh] Pull-to-refresh triggered.");
        var method = leaderboardScript.GetType().GetMethod("RefreshLeaderboardAsync");
        if (method != null)
        {
            Task task = (Task)method.Invoke(leaderboardScript, null);
            await task;
        }
        else
        {
            Debug.LogError("[PullToRefresh] RefreshLeaderboard method not found on leaderboardScript.");
        }

        isRefreshing = false;
        ResetIcon();
        scrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();

    }

    private void ResetIcon()
    {
        refreshIcon.canvasRenderer.SetAlpha(0);
        refreshIcon.transform.rotation = Quaternion.identity;
    }
}
