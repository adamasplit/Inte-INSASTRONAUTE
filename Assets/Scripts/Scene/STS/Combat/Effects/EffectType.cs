public enum EffectType
{
    Damage,
    Armor,
    Heal,
    Status,
    DeleteNextTurn,
    AdvanceTurn,
    DelayTurn,
    Multihit,
    Draw,
    Discard,
    Exhaust,
    LoseHP,
    GainEnergy,
    AddCardToHand,
    StealBuff,
    TransferDebuff,
    DispelBuff,
    DispelDebuff,
    EndTurn,
    Gravity,
    Break,
    AddRandomCard,
    AddCardToDrawPile,
    AddCardToDiscardPile,
    CardSelection,
    ForceNextCard,
    DoubleDebuffs,
    SetStatusToMaxValue,
    ExtendStatuses,
    DispelDebuffsIntoDamage,
    DispelBuffsIntoStatus,
    DispelSpecificStatus,
    AddCopyOfCard,
    CutInTurn,

    // Rejoue les effets qui la précèdent sur la même carte. Ajoutée en dernier : Unity sérialise
    // cet enum par index dans les ScriptableObjects, donc insérer ailleurs réécrirait
    // silencieusement l'effet de toutes les cartes situées après.
    ReplayCard,

    // Encadrent les statuts déjà posés sur la cible : un statut encadré échappe aux cartes
    // ordinaires, et le cadre d'or (trueEffect) échappe à tout. Ajoutées en dernier, pour la
    // même raison que ReplayCard.
    FrameBuffs,
    FrameDebuffs
}