using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Scene/Pack Data")]
public class PackData : ScriptableObject
{
    public string packId;
    public string packName;
    public int cardCount;
    public Sprite packSprite;

    public enum Rarity { Common, Rare, Epic, Legendary }

    [Header("Possible cards in this pack")]
    public List<PackCardEntry> possibleCards;
}

[System.Serializable]
public class PackCardEntry
{
    public string cardId;
    public PackData.Rarity rarity;
    public float weight = 1f; // pour plus tard (drop rate)
}
