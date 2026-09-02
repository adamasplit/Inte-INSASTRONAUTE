 using UnityEngine;
using System.Collections.Generic;
using System;
[CreateAssetMenu]
public class STSCardData : ScriptableObject
{
    public string id;
    public string cardName;
    public string collectionCardId;
    public bool multiplayerExclusive;
    /// <summary>Réservée à la campagne : jamais proposée par le constructeur de deck.</summary>
    public bool pveOnly;
    public int characterLevel;
    public Sprite icon;
    public int cost;
    public bool xCost=false;
    public CardType type;
    public CardRarity rarity;
    public List<EffectEntry> effects;
    public TargetingMode targetingMode;
    public List<ModifierData> modifiers = new();
    public SelectableCharacter favoredCharacter=SelectableCharacter.Aucun;
    public float animationSpeed=1f;
    public int startingCount=0;
    public List<CardTag> tags = new();
    public bool HasTag(CardTag tag)
    {
        return tags.Contains(tag);
    }
    public string GetCollectionCardId()
    {
        if (!string.IsNullOrWhiteSpace(collectionCardId))
            return collectionCardId;
        return null;
    }
    #if UNITY_EDITOR
    public void OnValidate()
    {
        id = name;
    }
    #endif
    public STSCardDataDTO ToDTO()
    {
        STSCardDataDTO dto = new();

        dto.id = id;
        dto.cardName = cardName;
        dto.collectionCardId = GetCollectionCardId();
        dto.multiplayerExclusive = multiplayerExclusive;
        dto.pveOnly = pveOnly;
        dto.characterLevel = characterLevel;
        dto.cost = cost;
        dto.iconId = icon != null ? icon.name : null;
        dto.type = type.ToString();

        dto.rarity = rarity.ToString();

        dto.targetingMode = targetingMode.ToString();
        dto.xCost = xCost;
        foreach (var tag in tags)
        {
            dto.tags.Add(tag.ToString());
        }

        dto.favoredCharacter = favoredCharacter.ToString();
        dto.animationSpeed = animationSpeed;
        dto.startingCount = startingCount;  
        foreach (var effect in effects)
        {
            dto.effects.Add(effect.ToDTO());
        }

        foreach (var mod in modifiers)
        {
            dto.modifiers.Add(mod.ToDTO());
        }

        return dto;
    }
    public static STSCardData FromDTO(STSCardDataDTO dto)
    {
        // CreateInstance et pas `new` : un ScriptableObject construit avec `new` fait
        // journaliser une erreur par carte à Unity (329 cartes = 329 erreurs au
        // démarrage) et rend un objet dont la moitié native n'existe pas.
        STSCardData card = CreateInstance<STSCardData>();

        card.id = dto.id;
        card.cardName = dto.cardName;
        if (dto.cardName == null || dto.cardName == "")
        {
            card.cardName = dto.id;
        }
        card.collectionCardId = dto.collectionCardId;
        card.multiplayerExclusive = dto.multiplayerExclusive;
        card.pveOnly = dto.pveOnly;
        card.characterLevel = dto.characterLevel;
        card.icon = (dto.iconId != null && dto.iconId != "") ? Resources.Load<Sprite>("STS/Icons/Cards/" + dto.iconId) : null;
        if (dto.iconId != null && dto.iconId != "" && card.icon == null)
        {
            Debug.LogError($"Icon with id {dto.iconId} not found for card {dto.id}");
            Debug.LogError($"Searched in path: STS/Icons/Cards/{dto.iconId}");
        }
        card.cost = dto.cost;

        card.type =
            Enum.Parse<CardType>(dto.type);

        card.rarity =
            Enum.Parse<CardRarity>(dto.rarity);

        card.targetingMode =
            Enum.Parse<TargetingMode>(dto.targetingMode);

        card.xCost = dto.xCost;
        card.animationSpeed = dto.animationSpeed;
        card.effects = new List<EffectEntry>();
        foreach (var effectDto in dto.effects)
        {
            card.effects.Add(EffectEntry.FromDTO(effectDto));
        }
        card.modifiers = new List<ModifierData>();
        foreach (var modDto in dto.modifiers)
        {
            card.modifiers.Add(ModifierData.FromDTO(modDto));
        }
        foreach(string tag in dto.tags)
        {
            if (Enum.TryParse<CardTag>(tag, out var parsedTag))
            {
                card.tags.Add(parsedTag);
            }
            else
            {
                Debug.LogWarning($"Unknown tag '{tag}' in card '{dto.id}'");
            }
        }
        card.favoredCharacter = Enum.Parse<SelectableCharacter>(dto.favoredCharacter);
        card.startingCount = dto.startingCount;
        return card;
    }
}