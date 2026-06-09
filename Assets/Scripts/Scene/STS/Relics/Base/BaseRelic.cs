using UnityEngine;
public abstract class BaseRelic:Relic
{
    /** Reliques de base
    * Stage 0 : version de base
    * Stage 1 : version améliorée (branche 1)
    * Stage 2 : version améliorée (branche 2)
    * Stage 3 : version améliorée (branche 1-1)
    * Stage 4 : version améliorée (branche 1-2)
    * Stage 5 : version améliorée (branche 2-1)
    * Stage 6 : version améliorée (branche 2-2)
    * Stage 7 : version ultime (plus rare)
    */
    public BaseRelic()
    {
        rarity = RelicRarity.Base;
        namesByStage = new string[8];
        descriptionsByStage = new string[8];
    }
    public int stage;
    public string[] namesByStage;
    public string[] descriptionsByStage;
    public void Upgrade(int stage)
    {
        this.stage = stage;
        string nameToSet = namesByStage[stage];
        if (nameToSet != null&& nameToSet != "")
            name=nameToSet;
        string descriptionToSet = descriptionsByStage[stage];
        if (descriptionToSet != null && descriptionToSet != "")
            description=descriptionsByStage[stage];
    }
    public int GetUpgradeStage()
    {
        return stage switch
        {
            0 => Random.Range(1, 3), // Upgrade to stage 1 or 2
            1 => Random.Range(3, 5), // Upgrade to stage 3 or 4
            2 => Random.Range(5, 7), // Upgrade to stage 5 or 6
            _ => 7 // Upgrade to stage 7 (ultimate) if already at stage 3-6
        };
    }
}