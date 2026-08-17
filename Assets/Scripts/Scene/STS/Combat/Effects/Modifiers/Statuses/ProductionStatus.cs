public class ProductionStatus : StatusEffect
{
    public ProductionStatus(int duration)
    {
        Duration = duration;
        Name = "Production";
        buff = true;
        framed = true;
    }
    public override void OnTurnStart(Character target)
    {
        target.DrawCard();
        Tick(target);
    }
    public override void OnTurnEnd(Character target)
    {
    }
    public override string Desc(bool isPlayer)
    {
        string turns = $"{Duration} tour" + (Duration > 1 ? "s" : "");
        if (isPlayer)
        {
            return $"Piochez 1 carte supplémentaire en début de tour pendant {turns}.";
        }
        return $"Le personnage pioche 1 carte supplémentaire en début de tour pendant {turns}.";
    }
}
