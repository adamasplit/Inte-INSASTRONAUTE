public enum ConditionType
{
    KillingBlow,
    ArmorBreak,
    FirstTimePlayingThisCardThisTurn,
    FirstTimePlayingThisCardThisCombat,
    TargetHasStatus,
    TargetHasNoStatus,
    SelfArmorThreshold,
    TargetArmorThreshold,
    EnergyGainedThreshold,
    EnergySpentThreshold,
    TargetWillAttack,
    TargetWillNotAttack,
    TargetHpHigherThanSelf,
    TargetHpLowerThanSelf,
    SelfBuffCountThreshold,
    TargetBuffCountThreshold,
    SelfDebuffCountThreshold,
    TargetDebuffCountThreshold,
    SelfHpMultiple,
    TargetHpMultiple,
    SelfArmorMultiple,
    TargetArmorMultiple,
    SelfTurnsBeforeTarget,

    // Ajoutées en dernier : cet enum est sérialisé par index dans les ScriptableObjects.
    // Écrites comme des plafonds et non des égalités, pour qu'une carte de dernier recours
    // « à 1 PV » reste vraie si l'on y arrive par en dessous.
    SelfHpAtMost,
    TargetHpAtMost
}