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
    public override void OnTurnStart(Character target)
    {
        target.TakeDamage(1);
    }
    public override string Desc(bool isPlayer)
    {
        return $"Inflige 1 dégât au début de chaque tour.";
    }
}
