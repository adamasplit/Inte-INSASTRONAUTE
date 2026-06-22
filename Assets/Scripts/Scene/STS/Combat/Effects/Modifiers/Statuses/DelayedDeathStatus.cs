public class DelayedDeathStatus : StatusEffect
{
    public DelayedDeathStatus(int duration)
    {
        Duration = duration;
        Name = "Mort retardée";
        buff=false;
        debuff=true;
        framed=true;
        goldFrame=true;
    }
    public override void OnExpire(Character target)
    {
        base.OnExpire(target);
        target.currentHP=0;
    }
    public override void OnTurnStart(Character target)
    {
        Tick(target);
    }
    public override void OnTurnEnd(Character target)
    {
    }
    public override string Desc(bool isPlayer)
    {
        if (Duration==1)
        {
            if (isPlayer)
            {
                return $"Vous mourrez au début de votre prochain tour";
            }
            return $"L'ennemi mourra au début de son prochain tour";
        }
        if (isPlayer)
        {
            return $"Vous serez tué dans {Duration} tour(s)";
        }
        return $"L'ennemi sera tué dans {Duration} tour(s)";
    }
}