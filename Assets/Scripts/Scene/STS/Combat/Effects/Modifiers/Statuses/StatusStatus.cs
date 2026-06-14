using UnityEngine;
public class StatusStatus : StatusEffect
{
    public StatusStatus(int duration)
    {
        Value=0;
        maxValue=0;
        Name = "Statuts";
        Duration = duration;
        debuff=true;
        framed=true;
    }
    public override void OnTurnEnd(Character target)
    {
        Debug.Log("StatusStatus OnTurnEnd triggered");
        switch (Random.Range(0, 3))
        {
            case 0:
                target.AddStatus(new StunStatus(1));
                break;
            case 1:
                target.AddStatus(new BlindStatus(1));
                break;
            case 2:
                target.AddStatus(new FullBreakStatus(1));
                break;
        }
    }
    public override string Desc()
    {
        return $"À la fin du tour, déclenche un effet de statut aléatoire : Étourdissement, Aveuglement, ou Déchéance sur la cible.";
    }
}