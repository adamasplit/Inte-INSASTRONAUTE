using UnityEngine;

public static class TestDatabase
{
    public static STSCardData attackCard;
    public static STSCardData blockCard;

    public static void Init()
    {
        attackCard=Resources.Load<STSCardData>("STS/Cards/Attaque");
        blockCard=Resources.Load<STSCardData>("STS/Cards/Défense");
    }
}