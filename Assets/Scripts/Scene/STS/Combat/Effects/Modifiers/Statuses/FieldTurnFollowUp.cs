using UnityEngine;
using System.Collections.Generic;
public class FieldTurnFollowUpStatus:FollowUpStatus
{
    public FieldTurnFollowUpStatus(int value,int duration,string effectInfo=""):base(value,duration,effectInfo)
    {
    }
    public override string Desc(bool isPlayer)
    {
        string maxValueStr = maxValue > 1 ? $"{maxValue}" : "";
        switch (moveIndex)
        {
            case 1: // End of own turn
                if (isPlayer)
                {
                    return $"Toutes les {maxValueStr} fins de votre tour, déclenchez {Name}.";
                }
                return $"Toutes les {maxValueStr} fins de son tour, l'ennemi joue aussi {Name}.";
            case 2: // Start of own turn
                if (isPlayer)
                {
                    return $"Tous les {maxValueStr} débuts de votre tour, déclenchez {Name}.";
                }
                return $"Tous les {maxValueStr} débuts de son tour, l'ennemi joue aussi {Name}.";
            default: // Any field turn end
                if (isPlayer)
                {
                    return $"Tous les {maxValueStr} tours, déclenchez {Name}.";
                }
                return $"Tous les {maxValueStr} tours, l'ennemi joue aussi {Name}.";
        }
    }
    public override void OnFieldTurnEnd(Character character)
    {
        if (moveIndex!=0) return;
        IncrementFollowUp(owner,character);
    }
    public override void OnTurnEnd(Character character)
    {
        if (moveIndex!=1) return;
        IncrementFollowUp(owner,character);
    }
    public override void OnTurnStart(Character character)
    {
        if (moveIndex!=2) return;
        IncrementFollowUp(owner,character);
    }
}