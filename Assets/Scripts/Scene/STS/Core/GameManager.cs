using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
public class GameManager : MonoBehaviour
{
    public UIManager ui;
    public CombatManager combat;
    public TurnSystem turnSystem;
    public List<STSCardData> cardsOnTest = new List<STSCardData>();
    
    async void Start()
    {
        STSSceneLoader.Instance?.BeginLoading();

        try
        {
            await STSCardDatabase.LoadAsync();
            await EnemyDataDatabase.LoadAsync();
            await EnemyPoolDatabase.LoadAsync();
            TestDatabase.Init();
            SetupGame();
            ui.Init(combat);
            turnSystem.Begin();
            ui.RefreshUI();
            combat.Init();
        }
        finally
        {
            STSSceneLoader.Instance?.EndLoading();
        }
    }

    void SetupGame()
    {
        if (RunManager.Instance == null||RunManager.Instance.forceTutorial)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
            combat.allies.Add(RunManager.Instance.player!=null ? RunManager.Instance.player : new Player("Player", 100));
            var enemies = new List<Character>
                    {
                        new Enemy("Dummy"),
                        new Enemy("Dummy"),
                        new Enemy("Dummy")
                    };
            combat.enemies = enemies;
            //combat.enemies = new List<Character>
            //        {
            //            new Enemy("Alexander"),
            //            new Enemy("Ark"),
            //            new Enemy("Golbez")
            //        };
            combat.deck = new DeckManager();

            // Ajout de cartes de test
            if (cardsOnTest.Count==0&&!RunManager.Instance.forceTutorial)
            {
                for (int i = 0; i < 1; i++)
                {
                    combat.deck.drawPile.Add(new CardInstance(TestDatabase.attackCard));
                    combat.deck.drawPile.Add(new CardInstance(TestDatabase.blockCard));
                }
                CardInstance enchantedCard = new CardInstance(TestDatabase.attackCard);
                enchantedCard.enchantments.Add(new CardEnchantment { data = new SharpnessEnchantment(), level = 10 });
                enchantedCard.enchantments.Add(new CardEnchantment { data = new MechanicalEnchantment(), level = 1 });
                combat.deck.drawPile.Add(enchantedCard);
                combat.deck.drawPile.AddRange(STSCardDatabase.allCards.Select(data => new CardInstance(data)));
                foreach (var card in combat.deck.drawPile)
                {
                    EnchantManager.ApplyEnchant(card,5);
                }
            }
            else
            {
                if (combat.forceTutorial||(RunManager.Instance!= null && RunManager.Instance.forceTutorial))
                {
                    STSCardData attackCard = STSCardDatabase.Get("Frappe");
                    STSCardData blockCard = STSCardDatabase.Get("Défense");
                    for (int i = 0; i < 5; i++)                    {
                        combat.deck.drawPile.Add(new CardInstance(attackCard));
                        combat.deck.drawPile.Add(new CardInstance(blockCard));
                    }
                    enemies = new List<Character>
                    {
                        new Enemy("Dummy"),
                        new Enemy("Dummy"),
                        new Enemy("Dummy")
                    };
                    combat.enemies = enemies;
                }
                else
                {
                    for (int i=0;i<10;i++)
                    {
                        RunManager.Instance.AddRelic(RelicDrop.GetRandomRelic(new CombatResult()));
                    }
                    foreach (var cardData in cardsOnTest)
                    {
                        combat.deck.drawPile.Add(new CardInstance(STSCardDatabase.Get(cardData.cardName)));
                    }
                    
                }
            }
            combat.deck.Shuffle(combat.deck.drawPile);
        }
        else
        {
            combat.allies.Add(RunManager.Instance.player);
            List<EnemyData> enemies = EnemySelector.GetRandomEncounter(RunManager.Instance.currentFloor, RunManager.Instance.eliteEncounter, RunManager.Instance.bossEncounter);
            combat.enemies = enemies.Select(e => (Character)new Enemy(e.enemyName)).ToList();
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