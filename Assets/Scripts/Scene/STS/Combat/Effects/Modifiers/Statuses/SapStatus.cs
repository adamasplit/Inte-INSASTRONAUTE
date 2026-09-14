using System.Collections.Generic;

public class SapStatus : StatusEffect
{
    public SapStatus()
    {
        Value = 0;
        Duration = -1;
        Name = "Sape";
        debuff = true;
        generic = true;
        framed = true;
    }
    // Chaque Sape est une instance à part, comme un Piège : deux Sapes mordent deux fois.
    public override void InsertInto(List<StatusEffect> list)
    {
        list.Add(this);
    }
    public override string Desc(bool isPlayer)
    {
        return $"Inflige 1 dégât chaque fois qu'un personnage joue une carte.";
    }
}
