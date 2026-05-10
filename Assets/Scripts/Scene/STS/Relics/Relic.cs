public abstract class Relic
{
    public RelicRarity rarity;
    public string name;
    public string description;
    public virtual void OnAcquire(Character player) {}
    public virtual void OnCombatStart(Character player) {}
    public virtual void OnCombatEnd(Character player) {}
    public virtual void OnTurnStart(Character player) {}
    public virtual void OnTurnEnd(Character player) {}
    public virtual void OnEnterRestSite(Character player) {}
    public string Describe()
    {
        if (description==""||description==null)
            return "Les informations sur cette relique sont indisponibles.";
        return $"{description}";
    }
}