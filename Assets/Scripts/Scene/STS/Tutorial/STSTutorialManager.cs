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
        if (RunManager.Instance != null && !RunManager.Instance.forceTutorial)
        {
            Debug.Log("RunManager présent mais forceTutorial à false, le tutoriel ne se lancera pas.");
            STSTutorialUI.Instance.Hide();
            return;
        }
        int value=0;
        if (RunManager.Instance!=null&&RunManager.Instance.forceTutorial)
        {
            value=RunManager.Instance.act;
        }
        else
        {
            Debug.Log("RunManager absent ou forceTutorial à false, lancement du tutoriel par défaut.");
        }
        BuildTutorialBase();
        BuildTutorialTimeline();
        Debug.Log($"Lancement du tutoriel avec value={value}");
        switch (value)
        {
            case 0:
                StartTutorial(nodes["intro"]);
                break;
            case 1:
                StartTutorial(nodes["timeline"]);
                break;
            default:
                StartTutorial(nodes["intro"]);
                break;
        }
    }
    public void BuildTutorialBase()
    {
        nodes["intro"] = new TutorialNode
        {
            text = "Bienvenue à INSASTRAL, où vous pouvez utiliser la vraie puissance de votre collection!",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["startCombat"]
        };
        nodes["startCombat"] = new TutorialNode
        {
            text = "Ce tutoriel couvrira les bases du combat.",

            onStart = () =>
            {
                
                pressed = false;
            },
            onComplete = () =>
            {
                combat.allowTurn = true;
            },

            condition = () => pressed,

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
            text = "Voici votre main. Au début de chaque tour, vous piochez 5 cartes. Vous pouvez sélectionner une carte pour voir sa description.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["playCard"]
        };
        nodes["playCard"] = new TutorialNode
        {
            text = "Glissez une carte sur une cible pour la jouer.",

            onStart = () =>
            {
                pressed = false;
                attackPlayed = false;
                defendPlayed = false;
                STSTutorialUI.Instance.HideOverlay();
            },

            condition = () => attackPlayed || defendPlayed,

            next = () =>
            {
                if (attackPlayed)
                    return nodes["attack"];

                if (defendPlayed)
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

            condition = () => pressed,

            next = () => nodes["explainAttack"]
        };
        nodes["defend"] = new TutorialNode
        {
            onStart = () =>
            {
                STSTutorialUI.Instance.ShowOverlay();
            },
            text = "Vous avez joué une compétence !",

            condition = () => pressed,

            next = () => nodes["explainDefend"]
        };
        nodes["endTurn"] = new TutorialNode
        {
            text = "Vous avez terminé votre tour ! Votre main a donc été défaussée.",

            onStart = () =>
            {
                pressed = false;
                STSTutorialUI.Instance.Unhighlight();
                STSTutorialUI.Instance.ShowOverlay();
            },

            condition = () => pressed,

            next = () => nodes["explainEndTurn"]
        };
        nodes["explainAttack"] = new TutorialNode
        {
            text = "Les attaques infligent toujours des dégâts à un ou plusieurs ennemis, mais peuvent avoir toutes sortes d'effets supplémentaires...",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["showEnergy"]
        };
        nodes["explainDefend"] = new TutorialNode
        {
            text = "Les compétences peuvent avoir divers effets, comme gagner de l'armure, se soigner ou appliquer des effets positifs/négatifs.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["explainArmor"]
        };
        nodes["explainArmor"] = new TutorialNode
        {
            text = "L'armure intercepte les dégâts que vous recevez, mais vous la perdez au début de votre prochain tour.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["explainArmor2"]
        };
        nodes["explainArmor2"] = new TutorialNode
        {
            text = "Les ennemis aussi peuvent gagner de l'armure: gardez un oeil sur leur barre de vie pour savoir combien.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["showEnergy"]
        };
        nodes["showEnergy"] = new TutorialNode
        {
            text = "Voici votre énergie: en jouant votre carte, vous en avez dépensé 1. Ce coût est indiqué en haut à gauche de la carte.",

            onStart = () =>
            {
                pressed = false;
                ui.HideOverlay();
                ui.Highlight(energyArea, canvas);
            },

            condition = () => pressed,

            next = () => nodes["explainEnergy"]
        };
        nodes["explainEnergy"] = new TutorialNode
        {
            text = "L'énergie est la ressource que vous dépensez pour jouer vos cartes. Vous en gagnez 3 au début de chaque tour.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["explainEnergy2"]
        };
        nodes["explainEnergy2"] = new TutorialNode
        {
            text = "Certaines cartes ou effets peuvent vous faire gagner de l'énergie supplémentaire, ou en perdre... Faites attention à votre gestion de l'énergie !",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () =>nodes["promptEndTurn"]
        };
        nodes["promptEndTurn"] = new TutorialNode
        {
            text = "Maintenant, terminez votre tour en appuyant sur le bouton 'Fin de tour' !",

            onStart = () =>
            {
                pressed = false;
                ui.HideOverlay();
                ui.Highlight(endTurnButton, canvas, 10f);
            },

            condition = () => turnEnded,

            next = () => nodes["endTurn"]
        };
        nodes["explainEndTurn"] = new TutorialNode
        {
            text = "Une fois que votre tour est terminé, les ennemis jouent à leur tour, puis vous revenez au début du tour suivant.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["explainIntents"]
        };
        nodes["explainIntents"] = new TutorialNode
        {
            text = "Avant que les ennemis ne jouent, vous pouvez voir leurs intentions: elles indiquent ce qu'ils prévoient de faire à leur prochain tour. Utilisez ces informations pour planifier votre stratégie !",

            onStart = () =>
            {
                pressed = false;
                ui.HideOverlay();
                ui.Highlight(FindFirstObjectByType<IntentUI>().GetComponent<RectTransform>(), canvas);
            },

            condition = () => pressed,

            next = () => nodes["explainIntents2"]
        };
        nodes["explainIntents2"] = new TutorialNode
        {
            text = "Ces intentions peuvent aussi être modifiées par des effets de cartes, comme affaiblir un ennemi pour réduire les dégâts qu'il inflige.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["globalExplanation"]
        };
        nodes["globalExplanation"] = new TutorialNode
        {
            text = "Il y a beaucoup plus à découvrir, mais c'est tout pour les bases ! Amusez-vous à explorer votre collection et à combattre des ennemis dans INSASTRAL !",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

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
            text = "Bienvenue dans ce tutoriel sur la timeline !",

            onStart = () =>
            {
                pressed = false;
                combat.allowTurn = true;
                ui.ShowOverlay();
            },

            condition = () => pressed,

            next = () => nodes["timelineExplanation"]
        };
        nodes["timelineExplanation"] = new TutorialNode
        {
            text = "La timeline vous montre l'ordre de jeu des personnages pendant le combat. Lorsque vous ciblez un personnage avec une carte, vous pouvez voir quels tours lui correspondent.",

            onStart = () =>
            {
                pressed = false;
                ui.Highlight(timelineArea,canvas);
                ui.HideOverlay();
            },

            condition = () => pressed,

            next = () => nodes["timelineExplanation2"]
        };
        nodes["timelineExplanation2"]=new TutorialNode
        {
            text = "Certains effets de cartes peuvent influencer la timeline, comme retarder un ennemi ou accélérer un allié. Ces modifications se reflètent dans la timeline: gardez un oeil dessus pour planifier vos actions !",

            onStart = () =>
            {
                pressed = false;
                combat.deck.AddCardToHand("Délai");
                combat.deck.AddCardToHand("Hâte");
            },

            condition = () => pressed,

            next = () => nodes["promptCardUse"]
        };
        nodes["promptCardUse"] = new TutorialNode
        {
            text = "Essayez de jouer ces cartes pour voir comment elles affectent la timeline !",

            onStart = () =>
            {
                pressed = false;
                delayPlayed = false;
                hastePlayed = false;
                ui.HideOverlay();
                ui.Unhighlight();
            },

            condition = () => delayPlayed || hastePlayed,

            next = () =>
            {
                if (delayPlayed)
                    return nodes["timelineDelay"];

                if (hastePlayed)
                    return nodes["timelineHaste"];

                return nodes["timelineDelay"];
            }
        };
        nodes["timelineDelay"] = new TutorialNode
        {
            text = "Vous avez retardé un ennemi, ce qui a repoussé son tour dans la timeline !",

            onStart = () =>
            {
                pressed = false;
                ui.ShowOverlay();
            },

            condition = () => pressed,

            next = () => nodes["timelineGlobal"]
        };
        nodes["timelineHaste"] = new TutorialNode
        {
            text = "Vous avez accéléré votre tour dans la timeline !",

            onStart = () =>
            {
                pressed = false;
                ui.ShowOverlay();
            },

            condition = () => pressed,

            next = () => nodes["timelineGlobal"]
        };
        nodes["timelineGlobal"]=new TutorialNode
        {
            text = "De nombreux effets de cartes peuvent influencer la timeline, et leur utilisation peut être cruciale.",

            onStart = () =>
            {
                pressed = false;
            },

            condition = () => pressed,

            next = () => nodes["timelineEnd"]
        };
        nodes["timelineEnd"] = new TutorialNode
        {
            text = "C'est tout pour ce tutoriel sur les tours !",

            onStart = () =>
            {
                pressed = false;
                ui.ShowOverlay();
                ui.Unhighlight();
            },

            condition = () => pressed,

            onComplete = () =>
            {
                STSSceneLoader.Instance.LoadScene("STS_Boot");
            },
            next = () => null
        };
    }

    public void StartTutorial(TutorialNode startNode)
    {
        current = startNode;
        EnterNode(current);
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
    private bool pressed;
    private bool attackPlayed;
    private bool defendPlayed;
    private bool delayPlayed;
    private bool hastePlayed;
    private bool turnEnded;
    float waitTimer;
    public void NotifyCardPlayed(CardInstance card)
    {
        if (card.data.type == CardType.Attaque)
            attackPlayed = true;
        else if (card.data.type == CardType.Compétence)
            defendPlayed = true;
        if (card.data.cardName == "Délai")
            delayPlayed = true;
        else if (card.data.cardName == "Hâte")
            hastePlayed = true;
    }
    public void NotifyTurnEnded()
    {
        turnEnded = true;
    }
    public void NotifyScreenPressed()
    {
        pressed = true;
    }
}