using UnityEngine;
public class CardRelic : Relic
{
    public CardRelic()
    {
        rarity = RelicRarity.Boss;
        name = "Léocarte";
        description = "Les récompenses de cartes peuvent contenir des cartes de n'importe quel personnage.";
    }
}