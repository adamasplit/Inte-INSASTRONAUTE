using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EventDatabase : ScriptableObject
{
    public List<EventDataSO> events;

    public static async Task<List<EventData>> LoadFromJsonAsync(string jsonPath)
    {
        string json = await StreamingAssetsLoader.ReadAllTextAsync(jsonPath);
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"Event JSON not found or unreadable: {jsonPath}");
            return null;
        }

        var wrapper = JsonUtility.FromJson<EventDataJsonExporter.EventDataListWrapper>(json);
        return wrapper != null ? wrapper.events : null;
    }

    public static List<EventData> LoadFromJson(string jsonPath)
    {
#if UNITY_ANDROID || UNITY_WEBGL
        Debug.LogError("EventDatabase.LoadFromJson() is not supported on Android/WebGL. Use LoadFromJsonAsync() and await it.");
        return null;
#else
        return LoadFromJsonAsync(jsonPath).GetAwaiter().GetResult();
#endif
    }
}
