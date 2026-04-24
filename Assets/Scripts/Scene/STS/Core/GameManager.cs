using System.Linq;
using UnityEngine;
using System.Collections.Generic;
public class GameManager : MonoBehaviour
{
    public UIManager ui;
    public CombatManager combat;
    public TurnSystem turnSystem;

    void Start()
    {
        STSCardDatabase.Init();
        TestDatabase.Init();
        SetupGame();
        ui.Init(combat);
        turnSystem.Begin();
        ui.RefreshUI();
    }

    void SetupGame()
    {
        combat.allies.Add(new Player("Player", 50));
        var enemies = new List<Character>
        {
            new Enemy("Enemy 1", 50),
            new Enemy("Enemy 2", 30),
            new Enemy("Enemy 3", 40)
        };
        combat.enemies = enemies;
        combat.deck = new DeckManager();

        // Ajout de cartes de test
        for (int i = 0; i < 3; i++)
        {
            combat.deck.drawPile.Add(new CardInstance(TestDatabase.attackCard));
            combat.deck.drawPile.Add(new CardInstance(TestDatabase.blockCard));
        }
        combat.deck.drawPile.AddRange(STSCardDatabase.allCards.Select(data => new CardInstance(data)));
        combat.deck.Shuffle(combat.deck.drawPile);
    }
}