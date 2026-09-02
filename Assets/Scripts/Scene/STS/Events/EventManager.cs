using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
            if (RunManager.Instance.IsServerAuthoritative && RunManager.Instance.activeEvent != null)
            {
                ShowServerEvent();
            }
            else
            {
                await LoadRandomEventAsync();
            }
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

    private void ShowServerEvent()
    {
        RunManager run = RunManager.Instance;
        if (run == null || run.activeEvent == null)
            return;

        if (description != null)
        {
            description.text = run.activeEvent["description"]?.ToString() ?? "";
        }
        image.sprite = null;

        string title = run.activeEvent["title"]?.ToString() ?? "Événement";
        var options = new List<PanelOption>();
        foreach (var optionToken in run.activeEvent["options"] ?? new JArray())
        {
            string optionId = optionToken["optionId"]?.ToString();
            if (string.IsNullOrWhiteSpace(optionId))
                continue;

            var option = new PanelOption
            {
                id = optionId,
                text = optionToken["text"]?.ToString() ?? optionId,
                completionMessage = optionToken["completionMessage"]?.ToString(),
                closePanel = true,
                entries = optionToken["entries"]?.ToObject<List<PanelOptionEntry>>()
                    ?? new List<PanelOptionEntry>()
            };
            option.action = () => SubmitEventChoice(option.id);
            options.Add(option);
        }

        panel.Show(title, options);
    }

    /// <summary>
    /// Point d'entrée depuis l'action d'option, qui est synchrone et ne peut pas
    /// attendre. L'échec est traité ici plutôt que remonté : rouvrir le panneau laisse
    /// le joueur retenter, là où avancer afficherait un gain que le serveur n'a pas
    /// accordé.
    /// </summary>
    /// <summary>
    /// Réaffiche les options que le serveur vient de poser dans <c>activeEvent</c>.
    /// C'est le cas d'une option qui en ouvre d'autres : le nœud reste en cours.
    /// </summary>
    private void ShowServerEventOptions()
    {
        RunManager run = RunManager.Instance;
        if (run == null || run.activeEvent == null)
            return;

        var options = new List<PanelOption>();
        foreach (var optionToken in run.activeEvent["options"] ?? new JArray())
        {
            string optionId = optionToken["optionId"]?.ToString();
            if (string.IsNullOrWhiteSpace(optionId))
                continue;

            var option = new PanelOption
            {
                id = optionId,
                text = optionToken["text"]?.ToString() ?? optionId,
                completionMessage = optionToken["completionMessage"]?.ToString(),
                closePanel = false,
                entries = optionToken["entries"]?.ToObject<List<PanelOptionEntry>>()
                    ?? new List<PanelOptionEntry>()
            };
            // L'action ne fait que renvoyer le choix : le serveur porte les effets.
            option.action = () => SubmitEventChoice(option.id);
            options.Add(option);
        }

        if (options.Count > 0)
            ReplaceEventOptions(options);
    }

    public void SubmitEventChoice(string optionId)
    {
        // Certaines entrées réclament que le joueur désigne des cartes — le serveur
        // refuse le choix sans la sélection attendue. C'est la seule chose que le
        // client décide encore ici, et c'est de l'interface : les effets restent au
        // serveur, on ne fait que lui dire sur quoi les appliquer.
        int required = RequiredCardSelection(optionId);
        if (required <= 0)
        {
            _ = SubmitEventChoiceAndRecoverAsync(optionId, null);
            return;
        }

        if (DeckSelectionPanel.Instance == null)
        {
            Debug.LogWarning($"[STS-EVENT] Option '{optionId}' demande {required} carte(s) mais aucun panneau de sélection n'est disponible.");
            return;
        }

        DeckSelectionPanel.Instance.Open(
            "Choisis les cartes",
            required,
            cards =>
            {
                var ids = new List<string>();
                foreach (CardInstance card in cards ?? new List<CardInstance>())
                {
                    if (card != null && !string.IsNullOrWhiteSpace(card.instanceId))
                        ids.Add(card.instanceId);
                }
                _ = SubmitEventChoiceAndRecoverAsync(optionId, ids);
            });
    }

    /// <summary>
    /// Combien de cartes l'option demande de désigner, d'après l'événement que le
    /// serveur a posé. Zéro quand elle n'en demande aucune.
    /// </summary>
    private int RequiredCardSelection(string optionId)
    {
        RunManager run = RunManager.Instance;
        if (run == null || run.activeEvent == null)
            return 0;

        foreach (var option in run.activeEvent["options"] ?? new JArray())
        {
            if (option["optionId"]?.ToString() != optionId)
                continue;

            foreach (var entry in option["entries"] ?? new JArray())
            {
                string type = entry["type"]?.ToString();
                if (type == "RemoveCard" || type == "UpgradeCard" || type == "TransformCard")
                    return entry["value"]?.Value<int>() ?? 0;
            }
        }

        return 0;
    }

    private async Task SubmitEventChoiceAndRecoverAsync(string optionId, List<string> selectedCardInstanceIds)
    {
        bool accepted = await SubmitEventChoiceAsync(optionId, selectedCardInstanceIds);
        if (accepted)
            return;

        Debug.LogWarning($"[STS-EVENT] Choix '{optionId}' non appliqué : panneau rouvert.");
        if (panel != null)
            panel.gameObject.SetActive(true);
    }

    /// <summary>
    /// Envoie l'option retenue au serveur, qui applique les effets et rend l'état.
    /// Rend <c>false</c> si l'appel échoue ou si le serveur refuse.
    /// </summary>
    public async Task<bool> SubmitEventChoiceAsync(string optionId, List<string> selectedCardInstanceIds)
    {
        RunManager run = RunManager.Instance;
        if (run == null || !run.IsServerAuthoritative || run.activeEvent == null)
            return true; // bac à sable : le moteur local a déjà appliqué

        string eventInstanceId = run.activeEvent["eventInstanceId"]?.ToString();
        if (string.IsNullOrWhiteSpace(eventInstanceId))
        {
            Debug.LogWarning("[STS-EVENT] Aucun eventInstanceId : choix non envoyé.");
            return false;
        }

        var request = new STSApiChooseEventOptionRequest
        {
            optionId = optionId,
            selectedCardInstanceIds = selectedCardInstanceIds ?? new List<string>()
        };

        try
        {
            Debug.Log($"[STS-EVENT] Envoi du choix optionId={optionId} eventInstanceId={eventInstanceId}");
            STSApiChooseEventOptionResponse response =
                await STSApiClient.ChooseEventOptionAsync(run.runId, eventInstanceId, request);

            if (response == null || !response.accepted)
            {
                Debug.LogWarning("[STS-EVENT] Choix refusé par le serveur.");
                return false;
            }

            run.ApplyEventChoiceResponse(response);

            // L'option en a ouvert d'autres : le serveur les a posées dans activeEvent,
            // on les réaffiche au lieu de refermer le nœud.
            if (!response.eventCompleted)
            {
                Debug.Log($"[STS-EVENT] L'événement continue : réaffichage des options.");
                // Le message de fin d'un palier est écrit pour être lu entre deux choix.
                // Il n'écrase la description que s'il dit quelque chose : la plupart des
                // options qui en ouvrent d'autres n'en portent pas, et les laisser passer
                // effaçait le texte de l'événement au premier choix.
                if (!string.IsNullOrWhiteSpace(response.completionMessage) && description != null)
                    description.text = response.completionMessage;
                ShowServerEventOptions();
                return true;
            }

            HideEventPanel();
            if (run.serverPendingRewards != null && run.serverPendingRewards.Count > 0)
            {
                STSSceneLoader.Instance?.LoadScene("STS_Reward");
                return true;
            }
            if (!string.IsNullOrWhiteSpace(response.completionMessage) && description != null)
                description.text = response.completionMessage;
            ShowEventContinue(ReturnToMap);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[STS-EVENT] Envoi du choix échoué : {ex.Message}");
            return false;
        }
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

    public async void ReturnToMap()
    {
        if (RunManager.Instance != null && RunManager.Instance.IsServerAuthoritative)
        {
            STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Map", "event_return");
            STSSceneLoader.Instance.LoadScene("STS_Map");
            return;
        }

        if (!await TryCompleteCurrentNodeAsync("event"))
        {
            return;
        }

        STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Map", "event_return");
        STSSceneLoader.Instance.LoadScene("STS_Map");
    }

    private async Task<bool> TryCompleteCurrentNodeAsync(string result)
    {
        if (RunManager.Instance == null || string.IsNullOrWhiteSpace(RunManager.Instance.runId) || RunManager.Instance.currentNode == null)
        {
            return true;
        }

        if (RunManager.Instance.unrestrictedMode)
        {
            return true;
        }

        // Sous autorité serveur, ChooseEventOption termine déjà le nœud côté serveur
        // (completed=true, enteredNodeId=null). Renvoyer CompleteNode échouerait en 409
        // car le nœud n'est plus en cours.
        if (RunManager.Instance.IsServerAuthoritative
            && (RunManager.Instance.currentNode.completed || !RunManager.Instance.enteredNodeId.HasValue))
        {
            return true;
        }

        var request = new STSApiNodeCompleteRequest
        {
            encounterInstanceId = null,
            result = result,
            turnCount = 0,
            playerHpAfter = RunManager.Instance.player != null ? RunManager.Instance.player.currentHP : 0,
            damageTaken = 0,
            enemiesDefeated = new List<string>(),
            deckHash = STSApiClient.ComputeDeckHash(RunManager.Instance.deck)
        };

        try
        {
            int nodeId = RunManager.Instance.currentNode.id;
            Debug.Log($"[STS-RUN] CompleteNode request (event) runId={RunManager.Instance.runId} nodeId={nodeId}");
            STSApiNodeCompleteResponse response = await STSApiClient.CompleteNodeAsync(RunManager.Instance.runId, nodeId, request);
            if (response != null && response.accepted)
            {
                Debug.Log($"[STS-RUN] CompleteNode response (event) accepted=true runId={response.runId} currentNodeId={response.currentNodeId}");
                RunManager.Instance.ApplyNodeCompleteResponse(response);
                if (RunManager.Instance.currentNode != null)
                {
                    RunManager.Instance.currentNode.completed = true;
                }
                return true;
            }

            Debug.LogWarning("[STS-RUN] CompleteNode response (event) was null or rejected. Staying in event scene to avoid desync.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-RUN] CompleteNode request (event) failed: {ex.Message}");
            return false;
        }
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