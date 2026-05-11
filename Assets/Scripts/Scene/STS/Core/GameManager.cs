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
            for (int i=0;i<10;i++)
            {
                RunManager.Instance.AddRelic(RelicDrop.GetRandomRelic(new CombatResult()));
            }
            combat.allies.Add(new Player("Player", 1500));
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
            CardInstance enchantedCard = new CardInstance(TestDatabase.attackCard);
            enchantedCard.enchantments.Add(new CardEnchantment { data = new SharpnessEnchantment(), level = 10 });
            combat.deck.drawPile.Add(enchantedCard);
            combat.deck.drawPile.AddRange(STSCardDatabase.allCards.Select(data => new CardInstance(data)));
            combat.deck.Shuffle(combat.deck.drawPile);
        }
        else
        {
            combat.allies.Add(RunManager.Instance.player);
            List<EnemyData> enemies = EnemySelector.GetRandomEncounter(RunManager.Instance.currentFloor, RunManager.Instance.eliteEncounter, RunManager.Instance.bossEncounter);
            combat.enemies = enemies.Select(e => (Character)new Enemy(e.enemyName) { data = e }).ToList();
            combat.deck = new DeckManager();
            foreach (CardInstance card in RunManager.Instance.deck)
            {
                combat.deck.drawPile.Add(card.Clone());
            }
            combat.deck.Shuffle(combat.deck.drawPile);
            combat.allies[0].statusEffects.Clear();
            foreach (Relic relic in RunManager.Instance.relics)
            {
                relic.OnCombatStart(combat.allies[0]);
            }
        }
    }
}