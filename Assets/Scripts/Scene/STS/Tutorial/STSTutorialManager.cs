using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class STSTutorialManager : MonoBehaviour
{
    public CombatManager combat;
    public STSTutorialUI ui;
    public Canvas canvas;
    public Camera mainCamera;
    public RectTransform endTurnButton;
    public RectTransform energyArea;
    public RectTransform timelineArea;
    TutorialNode current;
    Dictionary<string, TutorialNode> nodes = new Dictionary<string, TutorialNode>();
    public void Init()
    {
        bool tutorialEnabled = (combat != null && combat.forceTutorial) || (RunManager.Instance != null && RunManager.Instance.forceTutorial);
        if (!tutorialEnabled)
        {
            Debug.Log("Tutorial mode disabled, hiding tutorial UI.");
            STSTutorialUI.Instance.Hide();
            return;
        }
        ui.SetOverlayAlpha(0.6f);
        InitializeFlags();
        BuildTutorialBase();
        BuildTutorialTimeline();
        BuildTutorialMap();
        BuildTutorialStatus();
        StartTutorial(nodes["intro"]);
    }
    public void BuildTutorialBase()
    {
        nodes["intro"] = new TutorialNode
        {
            text = "Bienvenue à INSASTRAL, où vous pouvez utiliser la vraie puissance de votre collection!",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["startCombat"]
        };
        nodes["startCombat"] = new TutorialNode
        {
            text = "Ce tutoriel couvrira les bases d'une partie.",

            onStart = () =>
            {
                
                flags["pressed"] = false;
            },
            onComplete = () =>
            {
                combat.allowTurn = true;
            },

            condition = () => flags["pressed"],

            next = () => nodes["hand"]
        };
        nodes["wait"] = new TutorialNode
        {
            text = "",

            onStart = () =>
            {
                ui.Hide();
                waitTimer = 1.5f;
            },

            condition = () => waitTimer <= 0f,

            next = () => nodes["hand"]
        };
        nodes["hand"] = new TutorialNode
        {
            text = "Voici votre main. Au début de chaque tour, vous piochez 5 cartes. Vous pouvez sélectionner une carte ou la double-cliquer pour la voir en grand",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["playCard"]
        };
        nodes["playCard"] = new TutorialNode
        {
            text = "Glissez une carte sur une cible pour la jouer. (Pour vous cibler vous-même, glissez-la vers le bas de l'écran.)",

            onStart = () =>
            {
                flags["pressed"] = false;
                flags["attackPlayed"] = false;
                flags["defendPlayed"] = false;
                STSTutorialUI.Instance.HideOverlay();
            },

            condition = () => flags["attackPlayed"] || flags["defendPlayed"],

            next = () =>
            {
                if (flags["attackPlayed"])
                    return nodes["attack"];

                if (flags["defendPlayed"])
                    return nodes["defend"];

                return nodes["attack"];
            }
        };
        nodes["attack"] = new TutorialNode
        {
            onStart = () =>
            {
                STSTutorialUI.Instance.ShowOverlay();
            },
            text = "Vous avez joué une attaque !",

            condition = () => flags["pressed"],

            next = () => nodes["explainAttack"]
        };
        nodes["defend"] = new TutorialNode
        {
            onStart = () =>
            {
                STSTutorialUI.Instance.ShowOverlay();
            },
            text = "Vous avez joué une compétence !",

            condition = () => flags["pressed"],

            next = () => nodes["explainDefend"]
        };
        nodes["endTurn"] = new TutorialNode
        {
            text = "Vous avez terminé votre tour ! Votre main a donc été défaussée.",

            onStart = () =>
            {
                flags["pressed"] = false;
                STSTutorialUI.Instance.Unhighlight();
                STSTutorialUI.Instance.ShowOverlay();
            },

            condition = () => flags["pressed"],

            next = () => nodes["explainEndTurn"]
        };
        nodes["explainAttack"] = new TutorialNode
        {
            text = "Les attaques infligent toujours des dégâts à un ou plusieurs ennemis, mais peuvent avoir toutes sortes d'effets supplémentaires...",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["showEnergy"]
        };
        nodes["explainDefend"] = new TutorialNode
        {
            text = "Les compétences peuvent avoir divers effets, comme gagner de l'armure, se soigner ou appliquer des effets positifs/négatifs.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["explainArmor"]
        };
        nodes["explainArmor"] = new TutorialNode
        {
            text = "L'armure intercepte les dégâts que vous recevez, mais vous la perdez au début de votre prochain tour.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["explainArmor2"]
        };
        nodes["explainArmor2"] = new TutorialNode
        {
            text = "Les ennemis aussi peuvent gagner de l'armure: gardez un oeil sur leur barre de vie pour savoir combien.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["showEnergy"]
        };
        nodes["showEnergy"] = new TutorialNode
        {
            text = "Voici votre énergie: en jouant votre carte, vous en avez dépensé 1. Ce coût est indiqué en haut à droite de la carte.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.HideOverlay();
                ui.Highlight(energyArea, canvas);
            },

            condition = () => flags["pressed"],

            next = () => nodes["explainEnergy"]
        };
        nodes["explainEnergy"] = new TutorialNode
        {
            text = "L'énergie est la ressource que vous dépensez pour jouer vos cartes. Vous en gagnez 3 au début de chaque tour.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["explainEnergy2"]
        };
        nodes["explainEnergy2"] = new TutorialNode
        {
            text = "Certaines cartes ou effets peuvent vous faire gagner de l'énergie supplémentaire, ou en perdre... Faites attention à votre gestion de l'énergie !",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () =>nodes["promptEndTurn"]
        };
        nodes["promptEndTurn"] = new TutorialNode
        {
            text = "Maintenant, terminez votre tour en appuyant sur le bouton 'Fin de tour' !",

            onStart = () =>
            {
                flags["pressed"] = false;
                flags["turnEnded"] = false;
                ui.HideOverlay();
                ui.Highlight(endTurnButton, canvas, 10f);
            },

            condition = () => flags["turnEnded"],

            next = () => nodes["endTurn"]
        };
        nodes["explainEndTurn"] = new TutorialNode
        {
            text = "Une fois que votre tour est terminé, les ennemis jouent à leur tour, puis vous revenez au début du tour suivant.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["explainIntents"]
        };
        nodes["explainIntents"] = new TutorialNode
        {
            text = "Avant que les ennemis ne jouent, vous pouvez voir leurs intentions: elles indiquent ce qu'ils prévoient de faire à leur prochain tour. Utilisez ces informations pour planifier votre stratégie !",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.HideOverlay();
                ui.Highlight(FindFirstObjectByType<IntentUI>().GetComponent<RectTransform>(), canvas);
            },

            condition = () => flags["pressed"],

            next = () => nodes["explainIntents2"]
        };
        nodes["explainIntents2"] = new TutorialNode
        {
            text = "Ces intentions peuvent aussi être modifiées par des effets de cartes, comme affaiblir un ennemi pour réduire les dégâts qu'il inflige.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["timeline"]
        };
        nodes["globalExplanation"] = new TutorialNode
        {
            text = "Il y a beaucoup plus à découvrir, mais c'est tout pour les bases ! Amusez-vous à explorer votre collection et à combattre des ennemis dans INSASTRAL !",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.HideDummyMapPreview();
            },

            condition = () => flags["pressed"],

            onComplete = () =>
            {
            },
            next = () => null
        };
    }
    public void BuildTutorialMap()
    {
        nodes["mapIntro"] = new TutorialNode
        {
            text = "Voici une carte d'exemple. Chaque noeud représente un type de rencontre possible pendant votre partie.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowOverlay();
                ui.SetOverlayAlpha(1f);
                ui.ShowDummyMapPreview();
                ui.HighlightDummyMapNode(NodeType.Start, canvas, 10f);
            },

            condition = () => flags["pressed"],

            next = () => nodes["mapStart"]
        };
        nodes["mapStart"] = new TutorialNode
        {
            text = "Le noeud de départ marque le début de votre trajet sur la carte.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowDummyMapPreview();
                ui.HighlightDummyMapNode(NodeType.Start, canvas, 10f);
            },

            condition = () => flags["pressed"],

            next = () => nodes["mapCombat"]
        };
        nodes["mapCombat"] = new TutorialNode
        {
            text = "Un noeud de combat déclenche un affrontement standard et vous donne des récompenses de cartes.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowDummyMapPreview();
                ui.HighlightDummyMapNode(NodeType.Combat, canvas, 10f);
            },

            condition = () => flags["pressed"],

            next = () => nodes["mapElite"]
        };
        nodes["mapElite"] = new TutorialNode
        {
            text = "Les élites sont plus dangereuses, mais elles permettent d'obtenir des récompenses plus importantes. Le jeu en vaut la chandelle !",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowDummyMapPreview();
                ui.HighlightDummyMapNode(NodeType.Elite, canvas, 10f);
            },

            condition = () => flags["pressed"],

            next = () => nodes["mapRest"]
        };
        nodes["mapRest"] = new TutorialNode
        {
            text = "Dans un noeud de repos, vous avez 3 charges d'action: vous pouvez toutes les utiliser pour vous soigner, ou bien en utiliser pour améliorer vos cartes.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowDummyMapPreview();
                ui.HighlightDummyMapNode(NodeType.Rest, canvas, 10f);
            },

            condition = () => flags["pressed"],

            next = () => nodes["mapEvent"]
        };
        nodes["mapEvent"] = new TutorialNode
        {
            text = "Les noeuds d'évènement proposent souvent des choix et conséquences uniques.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowDummyMapPreview();
                ui.HighlightDummyMapNode(NodeType.Event, canvas, 10f);
            },

            condition = () => flags["pressed"],

            next = () => nodes["mapBoss"]
        };
        nodes["mapBoss"] = new TutorialNode
        {
            text = "Le boss termine la carte de l'acte et mène au combat final de cette zone. Il est inévitable.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowDummyMapPreview();
                ui.HighlightDummyMapNode(NodeType.Boss, canvas, 10f);
            },

            condition = () => flags["pressed"],

            next = () => nodes["mapWrapUp"]
        };
        nodes["mapWrapUp"] = new TutorialNode
        {
            text = "En lisant la carte à l'avance, vous pouvez planifier votre route entre combats, repos, évènements et boss.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.HideDummyMapPreview();
                ui.Unhighlight();
            },

            condition = () => flags["pressed"],

            next = () => nodes["globalMapExplanation"]
        };
        nodes["globalMapExplanation"] = new TutorialNode
        {
            text = "C'est tout pour le tutoriel  ! Bon courage !",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.HideDummyMapPreview();
                ui.Unhighlight();
            },

            condition = () => flags["pressed"],

            onComplete = () =>
            {
                STSSceneLoader.Instance.LoadScene("STS_Boot");
            },
            next = () => null
        };
    }

    public void BuildTutorialTimeline()
    {
        nodes["timeline"] = new TutorialNode
        {
            text = "Passons maintenant au système de tours.",
            onStart = () =>
            {
                combat.allowTurn = true;
                ui.ShowOverlay();
            },

            condition = () => flags["pressed"],

            next = () => nodes["timelineExplanation"]
        };
        nodes["timelineExplanation"] = new TutorialNode
        {
            text = "La timeline vous montre l'ordre de jeu des personnages pendant le combat. Lorsque vous ciblez un personnage avec une carte, vous pouvez voir quels tours lui correspondent.",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.Highlight(timelineArea,canvas);
                ui.HideOverlay();
            },

            condition = () => flags["pressed"],

            next = () => nodes["timelineExplanation2"]
        };
        nodes["timelineExplanation2"]=new TutorialNode
        {
            text = "Certains effets de cartes peuvent influencer la timeline, comme retarder un ennemi ou accélérer un allié. Ces modifications se reflètent dans la timeline: gardez un oeil dessus pour planifier vos actions !",

            onStart = () =>
            {
                flags["pressed"] = false;
                combat.deck.AddCardToHand("Délai");
                combat.deck.AddCardToHand("Hâte");
            },

            condition = () => flags["pressed"],

            next = () => nodes["promptCardUse"]
        };
        nodes["promptCardUse"] = new TutorialNode
        {
            text = "Essayez de jouer ces cartes (Hâte et Délai) tout en regardant la timeline pour voir comment elles l'affectent !",

            onStart = () =>
            {
                flags["pressed"] = false;
                flags["delayPlayed"] = false;
                flags["hastePlayed"] = false;
                ui.HideOverlay();
                ui.Unhighlight();
            },

            condition = () => flags["delayPlayed"] || flags["hastePlayed"],

            next = () =>
            {
                if (flags["delayPlayed"])
                    return nodes["timelineDelay"];

                if (flags["hastePlayed"])
                    return nodes["timelineHaste"];

                return nodes["timelineDelay"];
            }
        };
        nodes["timelineDelay"] = new TutorialNode
        {
            text = "Vous avez retardé un ennemi, ce qui a repoussé son tour dans la timeline !",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowOverlay();
            },

            condition = () => flags["pressed"],

            next = () => nodes["timelineGlobal"]
        };
        nodes["timelineHaste"] = new TutorialNode
        {
            text = "Vous avez accéléré votre tour dans la timeline !",

            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowOverlay();
            },

            condition = () => flags["pressed"],

            next = () => nodes["timelineGlobal"]
        };
        nodes["timelineGlobal"]=new TutorialNode
        {
            text = "De nombreux effets de cartes peuvent influencer la timeline, et leur utilisation peut être cruciale.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["timelineWarning"]
        };
        nodes["timelineWarning"]=new TutorialNode
        {
            text = "Restez vigilant: elle n'est pas totalement fiable et peut totalement changer en fonction des actions effectuées.",

            onStart = () =>
            {
                flags["pressed"] = false;
            },

            condition = () => flags["pressed"],

            next = () => nodes["statuses"]
        };
    }
    private void BuildTutorialStatus()
    {
        nodes["statuses"] = new TutorialNode
        {
            text = "Parlons maintenant des effets de statut .",
            onStart = () =>
            {
                combat.allowTurn = true;
                flags["pressed"] = false;
                ui.ShowOverlay();
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesExplanation"]
        };
        nodes["statusesExplanation"] = new TutorialNode
        {
            text = "Les effets de statut sont des effets qui peuvent être appliqués aux personnages et qui influencent le déroulement du combat. Ils peuvent être positifs ou négatifs (ou, plus rarement, neutres).",
            onStart = () =>
            {
                flags["pressed"] = false;
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesIconExplanation"]
        };
        nodes["statusesIconExplanation"] = new TutorialNode
        {
            text = "Les effets de statut sont représentés par des icônes en-dessous de la barre de vie, dont la couleur dépend de sa nature positive ou négative. Vous pouvez appuyer sur une icône pour voir le nom et la description de l'effet.",
            onStart = () =>
            {
                flags["pressed"] = false;
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesEnemyDemoIntro"]
        };
        nodes["statusesEnemyDemoIntro"] = new TutorialNode
        {
            text = "Regardez deux ennemis: leurs prochaines actions vont appliquer des effets de statut. Finissez votre tour pour voir quoi.",
            onStart = () =>
            {
                flags["pressed"] = false;
                flags["enemyForcePlayed"] = false;
                flags["enemyPoisonPlayed"] = false;
                ui.HideOverlay();
                QueueStatusEnemyDemo();
            },
            condition = () => flags["enemyForcePlayed"] && flags["enemyPoisonPlayed"],
            next = () => nodes["statusesPrompt"]
        };
        nodes["statusesPrompt"] = new TutorialNode
        {
            text = "Essayez maintenant de jouer les cartes 'Poison' et 'Force' pour voir les statuts qu'elles appliquent !",
            onStart = () =>
            {
                flags["pressed"] = false;
                flags["poisonPlayed"] = false;
                flags["forcePlayed"] = false;
                combat.deck.AddCardToHand("Poison");
                combat.deck.AddCardToHand("Force");
                ui.HideOverlay();
            },
            condition = () => flags["poisonPlayed"] || flags["forcePlayed"],
            next = () =>
            {
                if (flags["poisonPlayed"])
                    return nodes["statusesPoison"];

                if (flags["forcePlayed"])
                    return nodes["statusesForce"];

                return nodes["statusesPoison"];
            }
        };
        nodes["statusesPoison"] = new TutorialNode
        {
            text = "Vous avez appliqué le statut 'Poison' à un ennemi ! Le poison inflige des dégâts au début de chaque tour.",
            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowOverlay();
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesDispelIntro"]
        };
        nodes["statusesForce"] = new TutorialNode
        {
            text = "Vous avez appliqué le statut 'Force' ! La force augmente la puissance de certains coups et peut modifier l'impact d'une attaque.",
            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowOverlay();
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesDispelIntro"]
        };
        nodes["statusesDispelIntro"] = new TutorialNode
        {
            text = "Voici deux cartes utilitaires: Dissipation retire un buff, et Purification retire un debuff. Essayez-les sur la bonne cible.",
            onStart = () =>
            {
                flags["pressed"] = false;
                flags["dispelPlayed"] = false;
                flags["cleansePlayed"] = false;
                flags["dispelSucceeded"] = false;
                flags["cleanseSucceeded"] = false;

                PrepareDispelDemoTargets();
                combat.deck.AddCardToHand("Dissipation");
                combat.deck.AddCardToHand("Purification");
                ui.HideOverlay();
            },
            condition = () => flags["dispelPlayed"] || flags["cleansePlayed"],
            next = () =>
            {
                if (flags["dispelSucceeded"])
                    return nodes["statusesWrapUp"];
                if (flags["cleanseSucceeded"])
                    return nodes["statusesWrapUp"];
                if (flags["dispelPlayed"])
                    return nodes["statusesDispelRetry"];
                if (flags["cleansePlayed"])
                    return nodes["statusesCleanseRetry"];

                return nodes["statusesDispelRetry"];
            }
        };
        nodes["statusesDispelRetry"] = new TutorialNode
        {
            text = "Dissipation n'a rien retiré. Essayez-la sur l'ennemi qui a un buff.",
            onStart = () =>
            {
                flags["pressed"] = false;
                flags["dispelPlayed"] = false;
                flags["dispelSucceeded"] = false;
                combat.deck.AddCardToHand("Dissipation");
            },
            condition = () => flags["dispelPlayed"],
            next = () => {
                if (flags["dispelSucceeded"]) return nodes["statusesWrapUp"];
                return nodes["statusesDispelRetry2"];
            }
        };
        nodes["statusesDispelRetry2"] = new TutorialNode
        {
            text = "... Pas grave.",
            onStart = () =>
            {
                flags["pressed"] = false;
                combat.deck.AddCardToHand("Dissipation");
                ui.ShowOverlay();
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesWrapUp"]
        };
        nodes["statusesCleanseRetry"] = new TutorialNode
        {
            text = "Purification n'a rien retiré. Vous n'avez sans doute pas de debuff à retirer.",
            onStart = () =>
            {
                flags["pressed"] = false;
                combat.deck.AddCardToHand("Purification");
                ui.ShowOverlay();
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesWrapUp"]
        };
        nodes["statusesWrapUp"] = new TutorialNode
        {
            text="Certains effets ne peuvent pas être retirés par la plupart des cartes. Cela est indiqué par un cadre gris ou doré (les cadres dorés ne peuvent jamais être retirés par une carte).",
            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowOverlay();
            },
            condition = () => flags["pressed"],
            next = () => nodes["statusesEnd"]
        };
        nodes["statusesEnd"]=new TutorialNode
        {
            text = "Les statuts et leur dissipation peuvent changer le rythme d'un combat, que ce soit sur un ennemi, sur vous-même, ou via une action imposée par une carte.",
            onStart = () =>
            {
                flags["pressed"] = false;
                ui.ShowOverlay();
            },
            condition = () => flags["pressed"],
            next = () => nodes["mapIntro"]
        };
    }

    public void StartTutorial(TutorialNode startNode)
    {
        current = startNode;
        EnterNode(current);
    }
    private void InitializeFlags()
    {
        flags["pressed"] = false;
        flags["attackPlayed"] = false;
        flags["defendPlayed"] = false;
        flags["delayPlayed"] = false;
        flags["hastePlayed"] = false;
        flags["turnEnded"] = false;
        flags["enemyForcePlayed"] = false;
        flags["enemyPoisonPlayed"] = false;
        flags["poisonPlayed"] = false;
        flags["forcePlayed"] = false;
        flags["dispelPlayed"] = false;
        flags["cleansePlayed"] = false;
        flags["dispelSucceeded"] = false;
        flags["cleanseSucceeded"] = false;
    }

    private void QueueStatusEnemyDemo()
    {
        if (combat == null || combat.enemies == null)
        {
            Debug.LogWarning("Cannot queue tutorial enemy demo: combat or enemies list is null.");
            return;
        }

        Enemy firstEnemy = null;
        Enemy secondEnemy = null;

        foreach (var character in combat.enemies)
        {
            var enemy = character as Enemy;
            if (enemy == null || !enemy.IsAlive)
                continue;

            if (firstEnemy == null)
            {
                firstEnemy = enemy;
                continue;
            }

            secondEnemy = enemy;
            break;
        }

        if (firstEnemy != null)
        {
            firstEnemy.ForceNextAction("Force");
        }
        else
        {
            Debug.LogWarning("Tutorial enemy demo could not find a first alive enemy to force.");
        }

        if (secondEnemy != null)
        {
            secondEnemy.ForceNextAction("Poison");
        }
        else
        {
            Debug.LogWarning("Tutorial enemy demo could not find a second alive enemy to force.");
        }

        if (combat.ui != null)
        {
            combat.ui.RefreshUI(false);
        }
    }

    private void PrepareDispelDemoTargets()
    {
        if (combat == null)
        {
            return;
        }

        Enemy buffedEnemy = null;
        foreach (var character in combat.enemies)
        {
            buffedEnemy = character as Enemy;
            if (buffedEnemy != null && buffedEnemy.IsAlive)
            {
                break;
            }
        }

        if (buffedEnemy != null)
        {
            buffedEnemy.AddStatus(StatusEffect.Factory(StatusType.Strength, 1, -1));
        }

        if (combat.player != null)
        {
            combat.player.AddStatus(StatusEffect.Factory(StatusType.Weakness, 0, 2));
        }

        if (combat.ui != null)
        {
            combat.ui.RefreshUI(false);
        }
    }
    
    void Update()
    {
        if (waitTimer > 0f)
            waitTimer -= Time.deltaTime;

        if (current == null) return;

        if (current.condition != null && current.condition())
            GoNext();
    }

    void EnterNode(TutorialNode node)
    {
        ui.ShowText(node.text);
        node.onStart?.Invoke();
    }

    void GoNext()
    {
        current.onComplete?.Invoke();

        TutorialNode next = current.next?.Invoke();

        if (next == null)
        {
            ui.Hide();
            return;
        }

        current = next;
        EnterNode(current);
    }
    private Dictionary<string, bool> flags = new Dictionary<string, bool>();
    float waitTimer;
    public void NotifyCardPlayed(CardInstance card)
    {
        if (card.data.type == CardType.Attaque)
            flags["attackPlayed"] = true;
        else if (card.data.type == CardType.Compétence)
            flags["defendPlayed"] = true;
        if (card.data.cardName == "Délai")
            flags["delayPlayed"] = true;
        else if (card.data.cardName == "Hâte")
            flags["hastePlayed"] = true;
        if (card.data.cardName == "Poison")
            flags["poisonPlayed"] = true;
        else if (card.data.cardName == "Force")
            flags["forcePlayed"] = true;
        else if (card.data.cardName == "Dissipation")
            flags["dispelPlayed"] = true;
        else if (card.data.cardName == "Purification")
            flags["cleansePlayed"] = true;
    }

    public void NotifyEnemyCardPlayed(Enemy enemy, CardInstance card)
    {
        if (card == null || card.data == null)
            return;

        if (card.data.cardName == "Force")
            flags["enemyForcePlayed"] = true;
        else if (card.data.cardName == "Poison")
            flags["enemyPoisonPlayed"] = true;
    }

    public void NotifyDispelResult(string cardName, bool success)
    {
        if (cardName == "Dissipation")
        {
            flags["dispelSucceeded"] = success;
        }
        else if (cardName == "Purification")
        {
            flags["cleanseSucceeded"] = success;
        }

        if (success && combat != null && combat.ui != null)
        {
            combat.ui.RefreshUI(false);
        }
    }
    public void NotifyTurnEnded()
    {
        flags["turnEnded"] = true;
    }
    public void NotifyScreenPressed()
    {
        flags["pressed"] = true;
    }
}