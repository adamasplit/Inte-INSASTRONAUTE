using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ReactApiBridge : MonoBehaviour
{
    private const string WebBridgeGameObjectName = "WebBridge";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int Insastral_Request(string json);
#endif

    private static ReactApiBridge instance;
    private static readonly Dictionary<string, PendingRequest> pendingRequests = new Dictionary<string, PendingRequest>();

    private sealed class PendingRequest
    {
        public string name;
        public TaskCompletionSource<string> response;
    }

    private sealed class BridgeRequest
    {
        public string id;
        public string name;
        public object body;
    }

    private sealed class BridgeResponse
    {
        public string id;
        public bool ok;
        public JToken data;
        public JToken error;
    }

    private sealed class BridgeError
    {
        public string code;
        public string message;
    }

    private static ReactApiBridge EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<ReactApiBridge>();
        if (instance != null)
            return instance;

        var bridgeObject = new GameObject(WebBridgeGameObjectName);
        DontDestroyOnLoad(bridgeObject);
        instance = bridgeObject.AddComponent<ReactApiBridge>();
        return instance;
    }

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

    public static async Task<string> RequestAsync(string name, object body = null, int timeoutMs = 5000)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        instance = EnsureInstance();
        if (instance == null)
        {
            Debug.LogError("React bridge is not ready");
            return null;
        }

        var requestId = Guid.NewGuid().ToString();
        var pendingResponse = new TaskCompletionSource<string>();
        lock (pendingRequests)
        {
            pendingRequests[requestId] = new PendingRequest
            {
                name = name,
                response = pendingResponse
            };
        }

        Debug.Log($"Queued React bridge request '{name}' ({requestId}). Pending requests: {pendingRequests.Count}");

        var json = JsonConvert.SerializeObject(new BridgeRequest
        {
            id = requestId,
            name = name,
            body = body ?? new { }
        });

        Debug.Log($"Sending React bridge request '{name}': {requestId} | payload: {json}");

        var sent = Insastral_Request(json);
        if (sent == 0)
        {
            lock (pendingRequests)
            {
                pendingRequests.Remove(requestId);
            }
            Debug.LogError($"React bridge is not ready for request '{name}' ({requestId}).");
            return null;
        }

        var completed = await Task.WhenAny(pendingResponse.Task, Task.Delay(timeoutMs));
        if (completed != pendingResponse.Task)
        {
            lock (pendingRequests)
            {
                pendingRequests.Remove(requestId);
            }

            Debug.LogWarning($"Timed out waiting for React response for '{name}' ({requestId}). Pending requests may still be outstanding.");
            return null;
        }

        string response = await pendingResponse.Task;
        Debug.Log($"React bridge response received for '{name}' ({requestId}) | length: {(response != null ? response.Length : 0)}");
        return response;
#else
        Debug.LogWarning($"React bridge request '{name}' was skipped because this build is not running in WebGL.");
        return null;
#endif
    }

    public static async Task<string> RequestWithAliasesAsync(string[] names, object body = null, int timeoutMs = 5000)
    {
        if (names == null || names.Length == 0)
            return null;

        string lastResponse = null;
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (string.IsNullOrWhiteSpace(name))
                continue;

            string response = await RequestAsync(name, body, timeoutMs);
            if (string.IsNullOrWhiteSpace(response))
            {
                Debug.LogWarning($"React bridge request alias '{name}' returned no payload.");
                continue;
            }

            lastResponse = response;
            if (!IsUnknownRequestResponse(response, out string errorMessage))
            {
                Debug.Log($"React bridge accepted request alias '{name}'.");
                return response;
            }

            Debug.LogWarning($"React bridge rejected alias '{name}' as unknown request ({errorMessage}).");
        }

        Debug.LogError($"React bridge did not accept any request alias. Tried: {string.Join(", ", names)}");
        return lastResponse;
    }

    private static bool IsUnknownRequestResponse(string json, out string errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            BridgeResponse response = JsonConvert.DeserializeObject<BridgeResponse>(json);
            if (response == null)
                return false;

            if (response.ok)
                return false;

            if (response.error == null || response.error.Type == JTokenType.Null)
                return false;

            BridgeError error = response.error.ToObject<BridgeError>();
            if (error == null)
                return false;

            errorMessage = string.IsNullOrWhiteSpace(error.message) ? error.code : error.message;
            return string.Equals(error.code, "UNKNOWN_REQUEST", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static Task<string> RequestAdminCardsAsync(int timeoutMs = 5000)
    {
        return RequestAsync("admin.cards.list", null, timeoutMs);
    }

    public static Task<string> RequestAdminCharactersAsync(int timeoutMs = 5000)
    {
        return RequestAsync("admin.characters.list", null, timeoutMs);
    }

    public static Task<string> RequestStsCatalogCardsAsync(int timeoutMs = 5000)
    {
        return RequestWithAliasesAsync(
            new[] { "sts.catalog.cards", "catalog.cards.list" },
            null,
            timeoutMs
        );
    }

    public static Task<string> RequestStsCatalogCharactersAsync(int timeoutMs = 5000)
    {
        return RequestWithAliasesAsync(
            new[] { "sts.catalog.characters", "catalog.characters.list" },
            null,
            timeoutMs
        );
    }

    public static Task<string> RequestStsCatalogEnemiesAsync(int timeoutMs = 5000)
    {
        return RequestAsync("sts.catalog.enemies", null, timeoutMs);
    }

    public static Task<string> RequestStsCatalogEnemyPoolAsync(int timeoutMs = 5000)
    {
        return RequestAsync("sts.catalog.enemy-pool", null, timeoutMs);
    }

    public void RequestDeck()
    {
        _ = RequestAsync("cards.deck", new { collectionType = "VIRTUAL" });
    }

    public void HandleResponse(string json)
    {
        Debug.Log($"Response from React: {json}");

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("React bridge response was empty.");
            return;
        }

        string requestId = null;
        try
        {
            JObject response = JObject.Parse(json);
            requestId = response["id"]?.ToString();
            Debug.Log($"React bridge response parsed with id '{requestId}' and keys: {string.Join(", ", response.Properties())}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"React bridge response could not be parsed: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            Debug.LogWarning("React bridge response did not contain an id, so it could not be matched to a pending request.");
            return;
        }

        PendingRequest pendingRequest;
        lock (pendingRequests)
        {
            if (!pendingRequests.TryGetValue(requestId, out pendingRequest))
            {
                Debug.LogWarning($"React bridge response id '{requestId}' did not match any pending request.");
                return;
            }

            pendingRequests.Remove(requestId);
        }

        Debug.Log($"Completing React bridge request '{pendingRequest.name}' ({requestId}). Remaining pending requests: {pendingRequests.Count}");
        pendingRequest.response?.TrySetResult(json);
    }
    public void RequestAdminCards()
    {
        _ = RequestAdminCardsAsync();
    }

    public void RequestAdminCharacters()
    {
        _ = RequestAdminCharactersAsync();
    }

    public void RequestStsCatalogCards()
    {
        _ = RequestStsCatalogCardsAsync();
    }

    public void RequestStsCatalogCharacters()
    {
        _ = RequestStsCatalogCharactersAsync();
    }

    public void RequestStsCatalogEnemies()
    {
        _ = RequestStsCatalogEnemiesAsync();
    }

    public void RequestStsCatalogEnemyPool()
    {
        _ = RequestStsCatalogEnemyPoolAsync();
    }
}
