using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using System;
public class EventManager : MonoBehaviour
{
    public GenericPanel panel;
    public TextMeshProUGUI description;
    public UnityEngine.UI.Image image;
    public DeckSelectionPanel deckSelectionPanel;
    public EventRewardManager rewardManager;
    public string eventJsonPath = "Events/EventData.json";

    private List<EventData> loadedEvents;

    async void Start()
    {
        STSSceneLoader.Instance?.BeginLoading();

        try
        {
            if (RunManager.Instance == null)
            {
                await STSCardDatabase.LoadAsync();
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
            await LoadRandomEventAsync();
            STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, RunManager.Instance.currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "event_init");
        }
        finally
        {
            STSSceneLoader.Instance?.EndLoading();
            STSSceneLoader.Instance?.SceneReady();
        }
    }

    async Task LoadRandomEventAsync()
    {
        loadedEvents = await EventDatabase.LoadFromJsonAsync(eventJsonPath);
        if (loadedEvents == null || loadedEvents.Count == 0)
        {
            Debug.LogError("No events loaded from JSON!");
            return;
        }

        float totalWeight = 0;
        foreach (var ev in loadedEvents)
        {
            if (ev == null)
            {
                continue;
            }

            totalWeight += ev.weight;
        }

        if (totalWeight <= 0)
        {
            Debug.LogError("All loaded events had invalid weights.");
            return;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0;

        foreach (var ev in loadedEvents)
        {
            if (ev == null)
            {
                continue;
            }

            cumulative += ev.weight;
            if (roll < cumulative)
            {
                ShowEvent(ev);
                return;
            }
        }

        ShowEvent(loadedEvents[0]);
    }

    public void HideEventPanel()
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    public bool UpdateEventOption(string optionId, Action<PanelOption> mutator)
    {
        if (panel == null)
        {
            return false;
        }

        return panel.UpdateOption(optionId, mutator);
    }

    public bool ReplaceEventOption(string optionId, PanelOption replacement)
    {
        if (panel == null)
        {
            return false;
        }

        return panel.ReplaceOption(optionId, replacement);
    }

    public bool ReplaceEventOptions(List<string> targetIds, List<PanelOption> replacements, string fallbackOptionId)
    {
        if (panel == null)
        {
            return false;
        }

        return panel.ReplaceOptions(targetIds, replacements, fallbackOptionId);
    }

    public bool ReplaceCurrentEventOption(PanelOption currentOption, List<PanelOption> replacements)
    {
        if (panel == null)
        {
            return false;
        }

        return panel.ReplaceCurrentOption(currentOption, replacements);
    }

    public void ReplaceEventOptions(List<PanelOption> options)
    {
        panel?.ReplaceOptions(options);
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
            options.Add(opt.ToPanelOption(this)); // icon lookup by opt.iconName if needed
        }
        panel.Show(ev.title, options);
    }

    public void ReturnToMap()
    {
        STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Map", "event_return");
        STSSceneLoader.Instance.LoadScene("STS_Map");
    }

    public void ShowEventContinue(System.Action onComplete)
    {
        if (rewardManager != null)
        {
            rewardManager.ShowContinue(onComplete);
            return;
        }

        onComplete?.Invoke();
    }

    public void PresentReward(Reward reward, System.Action onComplete)
    {
        if (rewardManager != null)
        {
            rewardManager.ShowReward(reward, onComplete);
            return;
        }

        Debug.LogWarning("EventRewardManager is not assigned. Applying reward immediately.");

        foreach (var item in reward.items)
        {
            if (item is CardReward cardReward && cardReward.choices != null && cardReward.choices.Count > 0)
            {
                RunManager.Instance.deck.Add(cardReward.choices[0]);
                cardReward.Claim();
            }
            else if (item is RelicReward relicReward)
            {
                relicReward.Claim();
            }
            else if (item is GoldReward goldReward)
            {
                goldReward.Claim();
            }
            else if (item is BaseRelicUpgradeReward upgradeReward)
            {
                upgradeReward.Claim();
            }
        }

        onComplete?.Invoke();
    }
}