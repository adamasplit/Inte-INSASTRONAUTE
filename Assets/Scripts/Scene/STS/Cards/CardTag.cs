public enum CardTag
{
    None,
    Attack,
    Skill,
    Power,
    Status,
    Curse,
    Created,
    Unobtainable,
    FollowUp, // Indicates that this card is a follow-up to others and may not trigger other follow-up cards. It is used to prevent infinite loops of follow-up cards triggering each other.
    Exhaust,
    Retain,
    Ethereal,
    Innate,
    Infinite,
    Atom,
    Molecule,
    Norm,
    Developer,
    Automatic,
    Unplayable,
    // Ces deux-la restent en fin d'enum : CardRewardProfile serialise requiredTags/excludedTags
    // par ordinal, et inserer ailleurs redefinirait en silence les filtres deja poses sur les assets.
    SecretUnlock, // Jouer une carte qui le porte ouvre le pool secret, definitivement et pour tout le compte.
    Secret // Ne peut tomber en recompense qu'une fois le pool secret ouvert.
}