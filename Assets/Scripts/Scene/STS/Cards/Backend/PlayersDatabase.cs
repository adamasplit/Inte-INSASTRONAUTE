using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
public static class PlayersDatabase
{
    public static Dictionary<SelectableCharacter,PlayerInfoDTO> players = new Dictionary<SelectableCharacter, PlayerInfoDTO>();
    public static void Load()
    {

        string path =
            Path.Combine(Application.streamingAssetsPath, "Players");

        string[] files = Directory.GetFiles(path, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);

            PlayerInfoDTO dto =
                JsonConvert.DeserializeObject<PlayerInfoDTO>(json);

            players[Enum.Parse<SelectableCharacter>(dto.characterId)] = dto;
        }
        Debug.Log($"Loaded {players.Count} players.");
    }
    public static PlayerInfoDTO Get(SelectableCharacter character)
    {
        if (players.TryGetValue(character, out var player))
            return player;

        Debug.LogError($"Player {character} not found!");

        return null;
    }
}