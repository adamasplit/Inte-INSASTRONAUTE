using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/Collection Dataset")]
public class CardCollectionDataset : ScriptableObject
{
    public List<CardData> cards = new();
}
