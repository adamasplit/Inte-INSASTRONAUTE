using UnityEngine;
using System.Collections.Generic;
public class FieldTurnFollowUpStatus:StatusEffect
{
    private int moveIndex=0;
    public FieldTurnFollowUpStatus(int value,int duration,string effectInfo="")
    {
        Duration = -1;
        Name = effectInfo;
        Value = 1;
        moveIndex = value;
        maxValue=Mathf.Max(1, duration);
        debuff=false;
        buff=true;
        framed=true;
        STSCardData data = STSCardDatabase.Get(Name);
        if (data == null) 
        {
            mustExpire=true;
        }
        else
        {
            if (data.HasTag(CardTag.Status) || data.HasTag(CardTag.Curse))
            {
                buff=false;
                debuff=true;
            }
        }
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        StatusEffect other = list.Find(s => s.GetType() == this.GetType()&&s.Name==this.Name);
        if (other != null)
        {
        }
        else
        {
            list.Add(this);
        }
    }
    public override string Desc(bool isPlayer)
    {
        string maxValueStr = maxValue > 1 ? $"{maxValue}" : "";
        if (isPlayer)
        {
            return $"Tous les {maxValueStr} tours, déclenchez {Name}.";
        }
        return $"Tous les {maxValueStr} tours, l'ennemi joue aussi {Name}.";
    }
    public override void OnFieldTurnEnd(Character character)
    {
        Value++;
        if (Value>maxValue)
        {
            Value=1;
            STSCardData data = STSCardDatabase.Get(Name);
            if (data == null)
            {
                Debug.LogWarning($"Carte de suivi introuvable : {Name}");
                return;
            }
            CardInstance crystalCard = new CardInstance(data);
            owner.GetCombatManager().PlayCard(owner,crystalCard,owner.GetCombatManager().AutoCardTargets(crystalCard.targetingMode,owner,character),true,true);
        }
    }
}