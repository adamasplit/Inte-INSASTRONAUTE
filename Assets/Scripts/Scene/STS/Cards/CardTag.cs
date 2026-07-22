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
    Unplayable
}