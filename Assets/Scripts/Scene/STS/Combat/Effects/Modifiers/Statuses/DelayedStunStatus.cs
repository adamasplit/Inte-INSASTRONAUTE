using System.Collections.Generic;
public class DelayedStunStatus : StatusEffect
{
    private bool mustApplyStun = false;
    public DelayedStunStatus(int duration)
    {
        Duration = duration;
        Name = "Étourdissement retardé";
        buff=false;
        debuff=true;
    }
    public override void InsertInto(List<StatusEffect> list)
    {
        list.Add(this);
    }
    public override void OnExpire(Character target)
    {
        base.OnExpire(target);
        target.AddStatus(new StunStatus(1));
    }
    public override void OnDamageTaken(Character source,Character target, ref int damage)
    {
        Tick(target);
    }
    public override string Desc(bool isPlayer)
    {
        if (Duration==1)
        {
            if (isPlayer)
            {
                return $"Vous serez étourdi au prochain tour";
            }
            return $"L'ennemi sera étourdi au prochain tour";
        }
        if (isPlayer)
        {
            return $"Vous serez étourdi dans {Duration} tour(s) (cette valeur diminue aussi si vous subissez des dégâts)";
        }
        return $"L'ennemi sera étourdi dans {Duration} tour(s) (cette valeur diminue aussi si il subit des dégâts)";
    }
}