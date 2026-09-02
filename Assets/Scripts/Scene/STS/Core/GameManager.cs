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
        STSSceneLoader.Instance?.SetBackgroundProgress(0.05f);

        try
        {
            STSSceneLoader.Instance?.SetBackgroundProgress(0.12f);
            await STSCardDatabase.LoadAsync();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.40f);
            await EnemyDataDatabase.LoadAsync();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.62f);
            await EnemyPoolDatabase.LoadAsync();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.78f);
            TestDatabase.Init();
            SetupGame();
            STSSceneLoader.Instance?.SetBackgroundProgress(0.90f);
            ui.Init(combat);
            turnSystem.Begin();
            ui.RefreshUI();
            combat.Init();
            STSSceneLoader.Instance?.SetBackgroundProgress(1f);
        }
        finally
        {
            STSSceneLoader.Instance?.EndLoading();
        }
    }

    void SetupGame()
    {
        // Le duel d'abord : sans cette branche, ouvrir STS_Combat sans run tirerait une
        // rencontre PvE au hasard et un joueur de secours.
        if (RunManager.Instance != null
            && !string.IsNullOrWhiteSpace(RunManager.Instance.activePvpBattleId))
        {
            SetupPvpBattle();
            return;
        }

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
    /// <summary>
    /// Monte la scène pour un duel : deux combattants, un deck vide, aucune run.
    ///
    /// <para>Les points de vie, l'énergie, les statuts et les piles arrivent avec le
    /// premier état autoritatif ; ici on ne pose que les objets que l'interface a besoin
    /// d'avoir sous la main pour se construire. C'est la même division qu'en PvE, où
    /// EnsureEncounterEnemies pose les ennemis avant que l'état ne les remplisse.</para>
    ///
    /// <para><c>RunManager.player</c> n'est pas touché : une run PvE mise en pause pour
    /// jouer un duel doit se retrouver intacte.</para>
    /// </summary>
    /// <summary>
    /// Monte la scene d'une bataille multijoueur : un combattant par participant.
    ///
    /// <para>Un allie par participant de notre camp, un adversaire par participant d'en face.
    /// C'etait auparavant un seul de chaque, ecrit en dur, et c'est la seule chose qui empechait
    /// un 2v2 de s'afficher : le serveur envoyait bien quatre combattants, mais la scene n'avait
    /// que deux places ou les poser, et <c>CombatManager.ResolveCombatantByConvention</c> ne
    /// trouvait aucun objet pour les deux autres.</para>
    ///
    /// <para>Les points de vie sont un simple gabarit : le premier etat autoritatif apporte les
    /// vrais, comme il apporte les quatre piles.</para>
    /// </summary>
    void SetupPvpBattle()
    {
        RunManager run = RunManager.Instance;
        List<STSApiClient.StsPvpParticipantSnapshot> allies = run.AlliedPvpParticipants();
        List<STSApiClient.StsPvpParticipantSnapshot> opponents = run.OpposingPvpParticipants();

        const int PlaceholderHp = 1;

        combat.allies.Clear();
        // Notre siege vient en tete : le premier allie est celui dont la scene affiche la main.
        foreach (STSApiClient.StsPvpParticipantSnapshot participant in allies)
        {
            combat.allies.Add(new Player(PortraitNameOf(participant), PlaceholderHp)
            {
                playerDisplayName = participant != null ? participant.displayName : null,
                playerUserId = participant != null ? participant.combatantId : null
            });
        }

        combat.enemies = new List<Character>();
        foreach (STSApiClient.StsPvpParticipantSnapshot participant in opponents)
        {
            combat.enemies.Add(new Enemy(
                PortraitNameOf(participant),
                PlaceholderHp,
                participant != null ? participant.combatantId : null,
                participant != null ? participant.displayName : null));
        }

        // Un duel sans participants connus ne doit pas laisser la scene vide : le combat
        // deviendrait injouable sans que rien ne le dise.
        if (combat.allies.Count == 0)
        {
            Debug.LogWarning("[STS-PVP] No allied participant known for this battle; "
                + "falling back to a single placeholder player.");
            combat.allies.Add(new Player(SelectableCharacter.EP.ToString(), PlaceholderHp));
        }
        if (combat.enemies.Count == 0)
        {
            Debug.LogWarning("[STS-PVP] No opposing participant known for this battle; "
                + "falling back to a single placeholder opponent.");
            combat.enemies.Add(new Enemy(SelectableCharacter.EP.ToString(), PlaceholderHp, null, null));
        }

        // Vide : le premier état apporte les quatre piles telles que le serveur les tient.
        combat.deck = new DeckManager();

        Debug.Log($"[STS-PVP] Scene set up for battle {run.activePvpBattleId}: "
            + $"{combat.allies.Count} ally/allies vs {combat.enemies.Count} opponent(s) "
            + $"[{string.Join(", ", combat.enemies.ConvertAll(enemy => enemy.name))}]");
    }

    /// <summary>
    /// Le nom du portrait d'un participant, sous Resources/STS.
    ///
    /// <para>Pour un joueur c'est son personnage ; pour le boss d'un raid c'est l'ennemi dont il
    /// est tire, qui n'a pas de personnage et prendrait sinon le portrait d'EP.</para>
    /// </summary>
    static string PortraitNameOf(STSApiClient.StsPvpParticipantSnapshot participant)
    {
        return participant == null
            ? SelectableCharacter.EP.ToString()
            : participant.PortraitName;
    }
}
