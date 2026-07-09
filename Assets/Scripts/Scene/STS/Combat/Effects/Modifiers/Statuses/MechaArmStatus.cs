using UnityEngine;
public class MechaArmStatus : StatusEffect
{
    public MechaArmStatus(int value)
    {
        Value=value;
        Name = "Bras mécatronique";
        Duration = -1;
        buff=true;
        framed=true;
    }
    public override void OnTurnEnd(Character target)
    {
        Debug.Log("MechaArmStatus OnTurnEnd triggered");
        switch (Random.Range(0, 3))
        {
            case 0:
                target.Heal(1*Value);
                VFXManager.Instance.PlayEffect("Heal", target);
                break;
            case 1:
                target.AddArmor(4*Value);
                VFXManager.Instance.PlayEffect("Armor", target);
                break;
            case 2:
                foreach (var enemy in target.GetCombatManager().GetAdversaries(target))
                {
                    enemy.TakeDamage(3*Value);
                }
                VFXManager.Instance.PlayEffect("DamageMagic", target);
                break;
        }
    }
    public override string Desc(bool isPlayer)
    {
        return (isPlayer ? "À la fin de votre tour" : "À la fin du tour de l'ennemi") + $", déclenche un effet aléatoire : +{1*Value} PV, +{4*Value} Armure, ou {3*Value} dégâts sur tous les ennemis.";
    }
}