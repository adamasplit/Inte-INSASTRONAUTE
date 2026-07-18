using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class PlayersDatabase
{
    public static Dictionary<SelectableCharacter,PlayerInfoDTO> players = new Dictionary<SelectableCharacter, PlayerInfoDTO>();
    static bool isLoaded;
    static Task loadTask;

    public static async Task LoadAsync()
    {
        if (loadTask != null)
        {
            await loadTask;
            return;
        }

        loadTask = LoadInternalAsync();
        try
        {
            await loadTask;
        }
        finally
        {
            loadTask = null;
        }
    }

    static async Task LoadInternalAsync()
    {
        if (isLoaded)
            return;

        players.Clear();

        Debug.Log("PlayersDatabase loading remote character catalog through React bridge first.");
        if (!await TryLoadFromRemoteApiAsync())
        {
            await LoadFromStreamingAssetsAsync();
        }

        isLoaded = players.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("PlayersDatabase loaded zero entries. Check the remote character API and StreamingAssets/Players contents.");
        }
    }

    static async Task LoadFromStreamingAssetsAsync()
    {
        Debug.Log("PlayersDatabase falling back to StreamingAssets/Players.");
        List<string> files = await StreamingAssetsLoader.ListJsonFilesAsync("Players");
        Debug.Log($"PlayersDatabase found {files.Count} player JSON files in StreamingAssets.");

        foreach (string file in files)
        {
            string json = await StreamingAssetsLoader.ReadAllTextAsync(file);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"PlayersDatabase could not read '{file}'.");
                continue;
            }

            PlayerInfoDTO dto = JsonConvert.DeserializeObject<PlayerInfoDTO>(json);

            if (dto == null)
            {
                Debug.LogWarning($"Invalid player JSON in '{file}'.");
                continue;
            }

            if (!Enum.TryParse(dto.characterId, true, out SelectableCharacter character))
            {
                Debug.LogWarning($"PlayersDatabase could not map local characterId '{dto.characterId}' from '{file}' to SelectableCharacter.");
                continue;
            }

            players[character] = dto;
        }

        if (players.Count > 0)
        {
            Debug.Log($"PlayersDatabase loaded {players.Count} players from StreamingAssets.");
        }
        else
        {
            Debug.LogWarning("PlayersDatabase StreamingAssets fallback produced zero usable players.");
        }
    }

    static async Task<bool> TryLoadFromRemoteApiAsync()
    {
        Debug.Log("PlayersDatabase requesting character catalog (api/sts/catalog/characters) through React bridge.");
        string json = await ReactApiBridge.RequestStsCatalogCharactersAsync();
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("PlayersDatabase did not receive a character catalog payload from the React bridge.");
            return false;
        }

        Debug.Log($"PlayersDatabase received character catalog payload from React bridge ({json.Length} chars).");

        try
        {
            List<PlayerInfoDTO> remotePlayers = ParseRemotePlayers(json);
            if (remotePlayers == null || remotePlayers.Count == 0)
            {
                Debug.LogWarning("PlayersDatabase could not find a character list in the React bridge payload.");
                return false;
            }

            Debug.Log($"PlayersDatabase parsed {remotePlayers.Count} character entries from the remote payload.");
            foreach (PlayerInfoDTO dto in remotePlayers)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.characterId))
                {
                    Debug.LogWarning("PlayersDatabase encountered a null entry or missing characterId in the remote payload.");
                    continue;
                }

                if (!Enum.TryParse(dto.characterId, out SelectableCharacter character))
                {
                    Debug.LogWarning($"PlayersDatabase could not map remote characterId '{dto.characterId}' to SelectableCharacter.");
                    continue;
                }

                players[character] = dto;
            }

            if (players.Count > 0)
            {
                Debug.Log($"PlayersDatabase loaded {players.Count} players from remote API.");
                return true;
            }

            Debug.LogWarning("PlayersDatabase remote payload was parsed but produced zero usable players.");

            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to load players through the React bridge: {ex}");
            return false;
        }
    }

    static List<PlayerInfoDTO> ParseRemotePlayers(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        JToken root = JToken.Parse(json);
        return ParseRemotePlayers(root);
    }

    static List<PlayerInfoDTO> ParseRemotePlayers(JToken token)
    {
        if (token == null)
            return null;

        if (token.Type == JTokenType.Array)
        {
            return token.ToObject<List<PlayerInfoDTO>>();
        }

        if (token.Type != JTokenType.Object)
            return null;

        JObject rootObject = (JObject)token;
        string[] candidateKeys = new[] { "characters", "players", "data", "items", "result", "payload" };

        foreach (string key in candidateKeys)
        {
            if (rootObject.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken nestedToken))
            {
                List<PlayerInfoDTO> nestedPlayers = ParseRemotePlayers(nestedToken);
                if (nestedPlayers != null && nestedPlayers.Count > 0)
                {
                    return nestedPlayers;
                }
            }
        }

        foreach (JProperty property in rootObject.Properties())
        {
            if (property.Value.Type != JTokenType.Object && property.Value.Type != JTokenType.Array)
                continue;

            List<PlayerInfoDTO> nestedPlayers = ParseRemotePlayers(property.Value);
            if (nestedPlayers != null && nestedPlayers.Count > 0)
            {
                return nestedPlayers;
            }
        }

        return null;
    }

    public static void Load()
    {
#if UNITY_ANDROID || UNITY_WEBGL
        Debug.LogError("PlayersDatabase.Load() is not supported on Android/WebGL. Use LoadAsync() and await it.");
#else
        LoadAsync().GetAwaiter().GetResult();
#endif
    }

    public static async Task EnsureLoadedAsync()
    {
        if (players.Count > 0)
            return;

        isLoaded = false;
        await LoadAsync();
    }
    public static PlayerInfoDTO Get(SelectableCharacter character)
    {
        if (players.TryGetValue(character, out var player))
            return player;

        Debug.LogError($"Player {character} not found!");

        return null;
    }
}