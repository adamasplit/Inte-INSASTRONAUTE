using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.RemoteConfig;

public struct UserAttributes { }
public struct AppAttributes { }

public static class EventsRemoteConfig
{

    public static async Task<EventDto[]> GetEventsAsync()
    {
        Debug.Log("[Event] Fetching events from Remote Config...");
        
        try
        {
            // Ensure Remote Config is initialized
            if (RemoteConfigService.Instance == null)
            {
                Debug.LogError("[Event] RemoteConfigService.Instance is null!");
                return Array.Empty<EventDto>();
            }

            // Fetch Remote Config with explicit async wait
            var fetchTask = RemoteConfigService.Instance.FetchConfigsAsync(
                new UserAttributes(),
                new AppAttributes()
            );
            
            await fetchTask;

            Debug.Log("[Event] Fetch completed successfully");

            // Debug: List all available keys
            var allKeys = RemoteConfigService.Instance.appConfig.GetKeys();
            Debug.Log($"[Event] Available config keys: {string.Join(", ", allKeys)}");


            // Lire la clé JSON
            var json = RemoteConfigService.Instance.appConfig.GetJson("events_json");
            

            Debug.Log($"[Event] Raw JSON received: {json}");

            // Parser
            var wrapper = JsonUtility.FromJson<EventsWrapper>(json);

            if (wrapper == null || wrapper.events == null)
            {
                Debug.LogWarning("[Event] No events found or failed to parse JSON");
                return Array.Empty<EventDto>();
            }

            Debug.Log($"[Event] Parsed {wrapper.events.Length} events from JSON");

            // Filtrer + trier (V1 simple)
            var filteredEvents = wrapper.events
                .Where(e => e != null && e.enabled)
                .OrderByDescending(e => e.priority)
                .ToArray();

            Debug.Log($"[Event] Returning {filteredEvents.Length} enabled events (sorted by priority)");
            
            return filteredEvents;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Event] Error fetching events: {ex.Message}");
            Debug.LogException(ex);
            return Array.Empty<EventDto>();
        }
    }

    [Serializable]
    private class EventsWrapper
    {
        public EventDto[] events;
    }
}
