using UnityEngine;
public class EnergyStatus : StatusEffect
{
    public EnergyStatus(int value)    
    {
        Name="Énergie";
        Value = value;
        Duration = -1;
        framed=true;
        Update(null);
    }
    public override void Update(Character target)
    {
        if (Value < 0)
        {
            buff = false;
            debuff=true;
        }
        else if (Value > 0)
        {
            buff = true;
            debuff=false;
        }
        else
        {
            mustExpire = true;
        }
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Gagnez {Mathf.Abs(Value)} énergie en {(Value>0?"plus":"moins")} au début du tour.";
        }
        return $"L'ennemi gagne {Mathf.Abs(Value)} énergie en {(Value>0?"plus":"moins")} au début du tour.";
    }
    public override void OnTurnStart(Character target)
    {
        base.OnTurnStart(target);
        if (target.isPlayer)
        {
            target.GainEnergy(Value);
        }
    }
}