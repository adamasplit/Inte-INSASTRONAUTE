using UnityEngine;
public class SadismStatus:StatusEffect
{
    public SadismStatus()
    {
        Duration = -1;
        Name = "Préparation...";
        debuff=false;
        buff=true;
        framed=true;
        modifierType = ModifierType.Multiplicative;
    }
    public override void Update(Character target)
    {
        foreach (Character character in target.GetCombatManager().GetAdversaries(target))
        {
            if (character.currentHP <= character.maxHP *0.8f)
            {
                Debug.Log("Sadism evolved because " + character.name + " is below 80% HP (" + character.currentHP + "/" + character.maxHP + ")");
                target.AddStatus(new SadismIIStatus());
                this.mustExpire=true;
            }
        }
    }
    public override bool AppliesTo(StatType stat, EffectContext ctx)
    {
        return stat == StatType.Damage && ctx.source.statusEffects.Contains(this);
    }
    public override int Modify(int damage, EffectContext ctx)
    {
        return Mathf.FloorToInt(damage + (damage * 20) / 100);
    }
    public override string Desc(bool isPlayer)
    {
        return $"{20}% dégâts supplémentaires. Cet effet évolue lorsqu'un adversaire passe en-dessous de 80% de sa vie.";
    }
}