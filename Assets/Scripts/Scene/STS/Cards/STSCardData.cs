using UnityEngine;
using System.Collections.Generic;
using System;
[CreateAssetMenu]
public class STSCardData : ScriptableObject
{
    public string cardName;
    public CardData collectionCard;
    public int cost;
    public CardType type;
    public CardRarity rarity;
    public List<EffectEntry> effects;
    public TargetingMode targetingMode;
    public List<ModifierData> modifiers = new();
    public bool exhaust=false;
    public bool retain=false;
    #if UNITY_EDITOR
    private void OnValidate()
    {
        cardName = name;
    }
    #endif
    public STSCardDataDTO ToDTO()
    {
        STSCardDataDTO dto = new();

        dto.id = cardName;
        dto.collectionCardId = collectionCard != null ? collectionCard.cardId : "";
        dto.cost = cost;

        dto.type = type.ToString();

        dto.rarity = rarity.ToString();

        dto.targetingMode = targetingMode.ToString();

        dto.exhaust = exhaust;

        dto.retain = retain;

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

        card.cardName = dto.id;
        card.collectionCard = dto.collectionCardId=="" ? null : CardDatabase.Instance.Get(dto.collectionCardId);

        card.cost = dto.cost;

        card.type =
            Enum.Parse<CardType>(dto.type);

        card.rarity =
            Enum.Parse<CardRarity>(dto.rarity);

        card.targetingMode =
            Enum.Parse<TargetingMode>(dto.targetingMode);

        card.exhaust = dto.exhaust;

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

        return card;
    }
}