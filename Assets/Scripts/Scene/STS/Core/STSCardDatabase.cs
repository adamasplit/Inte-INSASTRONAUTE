using UnityEngine;
using System.Collections.Generic;
public static class STSCardDatabase
{
    public static List<STSCardData> allCards;
    public static void Init()
    {
        allCards = new List<STSCardData>(Resources.LoadAll<STSCardData>("STS/Cards"));
    }
}