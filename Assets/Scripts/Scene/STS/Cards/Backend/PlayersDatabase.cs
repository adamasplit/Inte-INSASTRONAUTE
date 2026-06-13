using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        await loadTask;
    }

    static async Task LoadInternalAsync()
    {
        if (isLoaded)
            return;

        players.Clear();

        List<string> files = await StreamingAssetsLoader.ListJsonFilesAsync("Players");

        foreach (string file in files)
        {
            string json = await StreamingAssetsLoader.ReadAllTextAsync(file);
            if (string.IsNullOrEmpty(json))
                continue;

            PlayerInfoDTO dto =
                JsonConvert.DeserializeObject<PlayerInfoDTO>(json);

            if (dto == null)
            {
                Debug.LogWarning($"Invalid player JSON in '{file}'.");
                continue;
            }

            players[Enum.Parse<SelectableCharacter>(dto.characterId)] = dto;
        }

        isLoaded = players.Count > 0;
        if (!isLoaded)
        {
            Debug.LogError("PlayersDatabase loaded zero entries. Check StreamingAssets/Players and its JSON contents.");
        }

        loadTask = null;
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