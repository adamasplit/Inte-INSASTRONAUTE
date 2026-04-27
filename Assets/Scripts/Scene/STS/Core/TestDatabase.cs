using UnityEngine;

public static class TestDatabase
{
    public static STSCardData attackCard;
    public static STSCardData blockCard;

    public static void Init()
    {
        attackCard=Resources.Load<STSCardData>("STS/Cards/Katana");
        blockCard=Resources.Load<STSCardData>("STS/Cards/Révision Model Text");
    }
}