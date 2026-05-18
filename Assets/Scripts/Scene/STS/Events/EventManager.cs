using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
public class EventManager : MonoBehaviour
{
    public GenericPanel panel;
    public TextMeshProUGUI description;
    public UnityEngine.UI.Image image;
    public DeckSelectionPanel deckSelectionPanel;
    public string eventJsonPath = "Assets/StreamingAssets/Events/EventData.json";

    private List<EventData> loadedEvents;

    void Start()
    {
        if (RunManager.Instance == null)
        {
            STSCardDatabase.Load();
            new GameObject("RunManager").AddComponent<RunManager>();
            for (int i = 0; i < 10; i++)
            {
                RunManager.Instance.AddRelic(RelicDrop.GetRandomRelic(new CombatResult()));
            }
            RunManager.Instance.player = new Player("Player", 1500);

            // Ajout de cartes de test
            TestDatabase.Init();
            CardInstance enchantedCard = new CardInstance(TestDatabase.attackCard);
            enchantedCard.enchantments.Add(new CardEnchantment { data = new SharpnessEnchantment(), level = 10 });
            enchantedCard.enchantments.Add(new CardEnchantment { data = new MechanicalEnchantment(), level = 1 });
            RunManager.Instance.deck.Add(enchantedCard);
            RunManager.Instance.deck.AddRange(STSCardDatabase.allCards.Select(data => new CardInstance(data)));
        }
        DeckSelectionPanel.Instance=this.deckSelectionPanel;
        LoadRandomEvent();
    }

    void LoadRandomEvent()
    {
        loadedEvents = EventDatabase.LoadFromJson(eventJsonPath);
        if (loadedEvents == null || loadedEvents.Count == 0)
        {
            Debug.LogError("No events loaded from JSON!");
            return;
        }
        int idx = Random.Range(0, loadedEvents.Count);
        ShowEvent(loadedEvents[idx]);
    }

    void ShowEvent(EventData ev)
    {
        description.text = ev.description;
        // image.sprite = ev.image; // You may want to resolve the sprite by name if needed
        image.sprite = null; // Placeholder: implement sprite lookup by ev.imageName if needed

        // Convert PanelOptionData to PanelOption for UI (actions must be assigned manually)
        var options = new List<PanelOption>();
        foreach (var opt in ev.options)
        {
            System.Action action = EventActionFactory.GetAction(opt, this);
            PanelOption panopt=opt.ToPanelOption();
            panopt.action = action;
            options.Add(panopt); // icon lookup by opt.iconName if needed
        }
        panel.Show(ev.title, options);
    }

    public void ReturnToMap()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("STS_Map");
    }
}