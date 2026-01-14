using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardId;

    [Header("UI")]
    public Sprite sprite;
    public Color borderColor;

    [Header("Gameplay")]
    public int rarity;
    public int FirstTimeValue;
    public int SubsequentValue;
    public string cardName;
    public string description;
}
