using System;
using UnityEngine;
using System.Linq;
public class SpecificStatusOnTargetModifier : StatModifier
{
    public StatusType statusType;
    public string statusName;
    public int perStatus = 2;
    public SpecificStatusOnTargetModifier(StatType type, int amount,string statusId)
    {
        this.type = type;
        this.statusType = Enum.Parse<StatusType>(statusId);
        perStatus = amount;
        statusName = StatusEffect.Factory(statusType,0,0).Name;
    }
    public override int Modify(int value, EffectContext ctx)
    {
        if (ctx.target == null)
            return value;
        StatusEffect status = ctx.target.statusEffects.FirstOrDefault(s => s.statusType == statusType);
        if (status == null)
            return value;
        return value + perStatus*Mathf.Max(1, Mathf.Max(0, status.Value)+Mathf.Max(0, status.Duration));
    }

    public override string Describe()
    {
        return $"{StatTypeString.ToFrench(type, perStatus,modifierType)} par {statusName} sur la cible";
    }
}