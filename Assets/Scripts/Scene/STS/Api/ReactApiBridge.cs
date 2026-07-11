using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;

public class ReactApiBridge : MonoBehaviour
{
    private const string WebBridgeGameObjectName = "WebBridge";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int Insastral_Request(string json);
#endif

    private static ReactApiBridge instance;
    private static TaskCompletionSource<string> pendingAdminCardsResponse;
    private static string pendingAdminCardsRequestId;

    private void Awake()
    {
        gameObject.name = WebBridgeGameObjectName;
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static async Task<string> RequestAdminCardsAsync(int timeoutMs = 5000)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (instance == null)
        {
            Debug.LogError("React bridge is not ready");
            return null;
        }

        if (pendingAdminCardsResponse != null)
        {
            Debug.LogWarning("React bridge already has a pending admin cards request");
            return null;
        }

        pendingAdminCardsResponse = new TaskCompletionSource<string>();
        var requestId = Guid.NewGuid().ToString();
        pendingAdminCardsRequestId = requestId;
        var json = "{\"id\":\"" + requestId + "\",\"name\":\"admin.cards.list\",\"body\":{}}";

        Debug.Log($"Sending admin cards request to React bridge: {requestId}");

        var sent = Insastral_Request(json);
        if (sent == 0)
        {
            pendingAdminCardsResponse = null;
            pendingAdminCardsRequestId = null;
            Debug.LogError("React bridge is not ready");
            return null;
        }

        var completed = await Task.WhenAny(pendingAdminCardsResponse.Task, Task.Delay(timeoutMs));
        if (completed != pendingAdminCardsResponse.Task)
        {
            pendingAdminCardsResponse = null;
            pendingAdminCardsRequestId = null;
            Debug.LogWarning("Timed out waiting for admin cards response from React");
            return null;
        }

        string response = await pendingAdminCardsResponse.Task;
        pendingAdminCardsResponse = null;
        pendingAdminCardsRequestId = null;
        Debug.Log("Admin cards response received from React bridge.");
        return response;
#else
        return null;
#endif
    }

    public void RequestDeck()
    {
        var json = "{\"id\":\"req-1\",\"name\":\"cards.deck\",\"body\":{\"collectionType\":\"VIRTUAL\"}}";
#if UNITY_WEBGL && !UNITY_EDITOR
        var sent = Insastral_Request(json);

        if (sent == 0)
        {
            Debug.LogError("React bridge is not ready");
        }
#else
        Debug.LogError("React bridge requests are only available in WebGL builds");
#endif
    }

    public void HandleResponse(string json)
    {
        Debug.Log($"Response from React{(string.IsNullOrWhiteSpace(pendingAdminCardsRequestId) ? string.Empty : $" for {pendingAdminCardsRequestId}")}: {json}");
        pendingAdminCardsResponse?.TrySetResult(json);
    }
    public void RequestAdminCards()
    {
        _ = RequestAdminCardsAsync();
    }
}
