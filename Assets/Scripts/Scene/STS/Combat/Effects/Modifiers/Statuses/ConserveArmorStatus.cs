public class ConserveArmorStatus : StatusEffect
{
    public ConserveArmorStatus(int value, int duration)
    {
        Duration = duration;
        Name = "Conservation d'armure";
        modifierType = ModifierType.Additive;
        buff=true;
        framed=true;
    }
    public override void OnTurnStart(Character character)
    {
        Tick(character);
    }
    public override void OnTurnEnd(Character character)
    {
    }
    public override string Desc(bool isPlayer)
    {
        return $"Vous ne perdez pas votre Armure au début du tour pendant {Duration} tour"+(Duration>1?"s":"");
    }
    public override int ArmorOnTurnStart(int previousArmor, Character character)
    {
        return previousArmor;
    }
}