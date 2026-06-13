using UnityEngine;
using System.Collections.Generic;
using System;
[CreateAssetMenu]
public class STSCardData : ScriptableObject
{
    public string id;
    public string cardName;
    public string collectionCardId;
    public CardData collectionCard;
    public Sprite icon;
    public int cost;
    public bool xCost=false;
    public CardType type;
    public CardRarity rarity;
    public List<EffectEntry> effects;
    public TargetingMode targetingMode;
    public List<ModifierData> modifiers = new();
    public bool exhaust=false;
    public bool retain=false;
    public bool innate=false;
    public bool infinite=false;
    public bool created=false; // Cards that are created during combat (e.g. by other cards) should have this set to true to avoid obtaining them outside of combat through the collection card system
    public SelectableCharacter favoredCharacter=SelectableCharacter.Aucun;
    public float animationSpeed=1f;
    public int startingCount=0;
    #if UNITY_EDITOR
    private void OnValidate()
    {
        id = name;
    }
    #endif
    public STSCardDataDTO ToDTO()
    {
        STSCardDataDTO dto = new();

        dto.id = id;
        dto.cardName = cardName;
        dto.collectionCardId = collectionCard != null ? collectionCard.cardId : collectionCardId;
        dto.cost = cost;
        dto.iconId = icon != null ? icon.name : null;

        dto.type = type.ToString();

        dto.rarity = rarity.ToString();

        dto.targetingMode = targetingMode.ToString();

        dto.exhaust = exhaust;

        dto.retain = retain;

        dto.innate = innate;
        dto.xCost = xCost;
        dto.infinite = infinite;
        dto.created = created;

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
        STSCardData card = new();

        card.id = dto.id;
        card.cardName = dto.cardName;
        if (dto.cardName == null || dto.cardName == "")
        {
            card.cardName = dto.id;
        }
        card.collectionCardId = dto.collectionCardId;
        card.collectionCard = (dto.collectionCardId==null||dto.collectionCardId=="") ? null : CardDatabase.Instance.Get(dto.collectionCardId);
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

        card.exhaust = dto.exhaust;
        card.xCost = dto.xCost;
        card.innate = dto.innate;
        card.infinite = dto.infinite;
        card.animationSpeed = dto.animationSpeed;
        card.created = dto.created;
        card.retain = dto.retain;
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

        card.favoredCharacter = Enum.Parse<SelectableCharacter>(dto.favoredCharacter);
        card.startingCount = dto.startingCount;
        return card;
    }
}