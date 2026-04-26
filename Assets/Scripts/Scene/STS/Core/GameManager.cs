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
        STSCardDatabase.Load();
        TestDatabase.Init();
        SetupGame();
        ui.Init(combat);
        turnSystem.Begin();
        ui.RefreshUI();
        combat.Init();
    }

    void SetupGame()
    {
        if (RunManager.Instance == null)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
            combat.allies.Add(new Player("Player", 50));
            var enemies = new List<Character>
            {
                new Enemy("Enemy 1"),
                new Enemy("Enemy 2"),
                new Enemy("Enemy 3")
            };
            combat.enemies = enemies;
            combat.deck = new DeckManager();

            // Ajout de cartes de test
            for (int i = 0; i < 1; i++)
            {
                combat.deck.drawPile.Add(new CardInstance(TestDatabase.attackCard));
                combat.deck.drawPile.Add(new CardInstance(TestDatabase.blockCard));
            }
            combat.deck.drawPile.AddRange(STSCardDatabase.allCards.Select(data => new CardInstance(data)));
            combat.deck.Shuffle(combat.deck.drawPile);
        }
        else
        {
            combat.allies.Add(RunManager.Instance.player);
            List<EnemyData> enemies = EnemySelector.GetRandomEncounter(RunManager.Instance.currentFloor, RunManager.Instance.eliteEncounter, RunManager.Instance.bossEncounter);
            combat.enemies = enemies.Select(e => (Character)new Enemy(e.enemyName) { data = e }).ToList();
            Debug.Log($"Selected enemies: {string.Join(", ", combat.enemies.Select(e => e.name))}");
            RunManager.Instance.eliteEncounter = false;
            RunManager.Instance.bossEncounter = false;
            combat.deck = new DeckManager();
            foreach (var cardData in RunManager.Instance.deck)
            {
                combat.deck.drawPile.Add(new CardInstance(cardData));
            }
            combat.deck.Shuffle(combat.deck.drawPile);
        }
    }
}