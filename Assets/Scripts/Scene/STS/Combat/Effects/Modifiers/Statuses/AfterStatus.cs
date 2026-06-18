public class AfterStatus : StatusEffect
{
    public AfterStatus(int value)    
    {
        Name="After";
        Value = value;
        Duration = 1;
        inextendable = true;
        debuff=true;
    }
    public override string Desc(bool isPlayer)
    {
        if (isPlayer)
        {
            return $"Vous subissez {Value} dégâts à la fin de votre tour.";
        }
        return $"La cible subit {Value} dégâts à la fin de son tour.";
    }
    public override void OnTurnEnd(Character target)
    {
        base.OnTurnEnd(target);
        target.TakeDamage(Value);
    }
}