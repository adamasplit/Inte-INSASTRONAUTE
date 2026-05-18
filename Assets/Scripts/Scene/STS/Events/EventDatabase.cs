using System.Collections.Generic;
using UnityEngine;

public class EventDatabase : ScriptableObject
{
    public List<EventDataSO> events;

    public static List<EventData> LoadFromJson(string jsonPath)
    {
        if (!System.IO.File.Exists(jsonPath))
        {
            Debug.LogError($"Event JSON not found: {jsonPath}");
            return null;
        }
        string json = System.IO.File.ReadAllText(jsonPath);
        var wrapper = JsonUtility.FromJson<EventDataJsonExporter.EventDataListWrapper>(json);
        return wrapper != null ? wrapper.events : null;
    }
}
