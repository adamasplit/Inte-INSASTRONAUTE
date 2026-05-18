public abstract class Relic
{
    public RelicRarity rarity=RelicRarity.Base;
    public string name;
    public string description;
    public virtual int ArmorOnTurnStart(int previousArmor, Character character) { return 0; }
    public virtual int EnergyOnTurnStart(int previousEnergy, Character character) { return 0; }
    public virtual void OnAcquire(Character player) {}
    public virtual void OnCombatStart(Character player) {}
    public virtual void OnCombatEnd(Character player) {}
    public virtual void OnAnyTurnStart(Character character) {}
    public virtual void OnAnyTurnEnd(Character character) {}
    public virtual void OnTurnStart(Character player) {}
    public virtual void OnTurnEnd(Character player) {}
    public virtual void OnEnterRestSite(Character player) {}
    public virtual void OnEnterShop(Character player) {}
    public virtual void OnOwnArmorBroken(Character source, Character target) {}
    public virtual void OnTargetArmorBroken(Character source, Character target) {}
    public virtual void OnDamageDealt(Character source, Character target, int amount) {}
    public virtual void OnDamageTaken(Character source, Character target, int amount) {}
    public virtual void OnAnyArmorGain(Character target, int amount) {}
    public string Describe()
    {
        if (description==""||description==null)
            return "Les informations sur cette relique sont indisponibles.";
        return $"{description}";
    }
}