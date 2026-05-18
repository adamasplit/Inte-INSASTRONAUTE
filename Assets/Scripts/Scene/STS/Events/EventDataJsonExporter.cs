using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EventDataJsonExporter
{
#if UNITY_EDITOR
    [MenuItem("Tools/Export EventDataSO to JSON")]
    public static void ExportAllEventDataSOToJson()
    {
        string[] guids = AssetDatabase.FindAssets("t:EventDataSO");
        List<EventData> eventDatas = new List<EventData>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EventDataSO so = AssetDatabase.LoadAssetAtPath<EventDataSO>(path);
            if (so != null)
            {
                eventDatas.Add(new EventData(so));
            }
        }
        string json = JsonUtility.ToJson(new EventDataListWrapper(eventDatas), true);
        File.WriteAllText("Assets/StreamingAssets/Events/EventData.json", json);
        Debug.Log($"Exported {eventDatas.Count} EventDataSO to JSON.");
    }
#endif

    [System.Serializable]
    public class EventDataListWrapper
    {
        public List<EventData> events;
        public EventDataListWrapper(List<EventData> events) { this.events = events; }
    }
}
