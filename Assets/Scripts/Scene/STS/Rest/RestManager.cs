using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.UI;
using System;
public class RestManager : MonoBehaviour
{
    public GenericPanel enchantPanel;
    public Transform deckContainer;
    public GameObject cardPrefab;
    public GameObject chargePrefab;
    public Transform chargesContainer;
    private float deckContentPadding = 48f;
    private float enchantPreviewDelay = 1f;
    private float enchantExitDuration = 1f;
    [SerializeField] private float enchantExitScreenMargin = 64f;
    CardInstance selectedCard;
    RestCardController selectedController;
    bool isEnchanting;
    readonly STSCompletionGate completionGate = new STSCompletionGate();

    async void Start()
    {
        if (RunManager.Instance==null)
        {
            GameObject newRunManager = new GameObject("RunManager");
            newRunManager.AddComponent<RunManager>();
            await RunManager.Instance.StartRunAsync("",50,new List<Relic>{},false);
        }
        // Sous autorité serveur, les charges et les reliques de repos ont déjà été
        // appliquées à l'entrée du nœud : les rejouer ici les compterait deux fois.
        if (!RunManager.Instance.IsServerAuthoritative)
        {
            RunManager.Instance.restCharges = STSRestState.InitialCharges(
                RunManager.Instance.enteredNodeId.HasValue
                    && RunManager.Instance.currentNode != null
                    && RunManager.Instance.currentNode.type == NodeType.Rest,
                RunManager.Instance.restCharges,
                RunManager.Instance.maxRestCharges);
            foreach (var relic in RunManager.Instance.relics)
            {
                relic.OnEnterRestSite(RunManager.Instance.player);
            }
        }
        BuildDeck();
        UpdateChargesDisplay();
        STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, RunManager.Instance.currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "rest_init");
        STSSceneLoader.Instance?.EndLoading();
        STSSceneLoader.Instance?.SceneReady();
    }

    void BuildDeck()
    {
        foreach (Transform child in deckContainer)
            Destroy(child.gameObject);
        foreach (var card in RunManager.Instance.deck)
        {
            if (card.isEnchanted())
                continue; // Skip enchanted cards in the rest site
            var obj = Instantiate(cardPrefab, deckContainer);

            var ctrl = obj.GetComponent<RestCardController>();
            ctrl.Init(card, this);
        }

        StartCoroutine(RefreshDeckContainerAfterFrame());
    }

    // ---------------- HEAL ----------------
    public void OnRest()
    {
        if (RunManager.Instance.IsServerAuthoritative)
        {
            _ = RestOnServerAsync();
            return;
        }

        while (RunManager.Instance.restCharges > 0&&RunManager.Instance.player.currentHP<RunManager.Instance.player.maxHP)
        {
            RunManager.Instance.player.Heal(Mathf.FloorToInt(RunManager.Instance.player.maxHP/12f));
            RunManager.Instance.restCharges--;
        }
        UpdateChargesDisplay();
    }

    /// <summary>
    /// Demande le soin au serveur. Un échec ne touche à rien : les charges restent
    /// telles quelles et le joueur peut réessayer, ce qu'un soin appliqué en local
    /// « au cas où » lui interdirait en désynchronisant les deux états.
    /// </summary>
    private async Task RestOnServerAsync()
    {
        RunManager run = RunManager.Instance;
        try
        {
            STSApiRestActionResponse response = await STSApiClient.RestAsync(
                run.runId,
                run.enteredNodeId ?? -1,
                new STSApiRestActionRequest { action = "HEAL" });

            if (response == null || !response.accepted)
            {
                Debug.LogWarning("[STS-REST] Soin refusé par le serveur.");
                return;
            }

            run.ApplyRestResponse(response);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[STS-REST] Soin échoué : {ex.Message}");
            return;
        }

        UpdateChargesDisplay();
    }

    // ---------------- ENCHANT ----------------
    public void OnCardSelected(RestCardController controller)
    {
        if (controller == null)
            return;

        foreach (Transform child in deckContainer)
        {
            RestCardController cardController = child.GetComponent<RestCardController>();
            if (cardController != null)
                cardController.SetSelected(false);
        }

        selectedController = controller;
        selectedCard = controller.Card;
        selectedController.SetSelected(true);

        var options = new List<PanelOption>();

        options.Add(new PanelOption
        {
            text = "Niveau 1 (1 charge)",
            icon = Resources.Load<Sprite>("STS/Icons/Enchant1"),
            action = () => {OnEnchant(1); }
        });

        options.Add(new PanelOption
        {
            text = "Niveau 2 (2 charges)",
            icon = Resources.Load<Sprite>("STS/Icons/Enchant2"),
            action = () => OnEnchant(2)
        });

        options.Add(new PanelOption
        {
            text = "Niveau 3 (3 charges)",
            icon = Resources.Load<Sprite>("STS/Icons/Enchant3"),
            action = () => OnEnchant(3)
        });

        enchantPanel.gameObject.SetActive(true);
        enchantPanel.Show("Enchanter la carte", options);
    }

    public void OnEnchant(int charges)
    {
        if (isEnchanting)
            return;

        StartCoroutine(EnchantRoutine(charges));
    }

    /// <summary>
    /// Envoie l'enchantement au serveur, qui tire le niveau et l'applique à la carte.
    /// Rend <c>false</c> si l'appel échoue ou si le serveur refuse — la carte reste
    /// alors telle quelle, et les charges avec.
    /// </summary>
    private async Task<bool> EnchantOnServerAsync(int charges)
    {
        RunManager run = RunManager.Instance;
        try
        {
            STSApiRestActionResponse response = await STSApiClient.RestAsync(
                run.runId,
                run.enteredNodeId ?? -1,
                new STSApiRestActionRequest
                {
                    action = "ENCHANT",
                    cardInstanceId = selectedCard != null ? selectedCard.instanceId : null,
                    charges = charges
                });

            if (response == null || !response.accepted)
            {
                Debug.LogWarning("[STS-REST] Enchantement refusé par le serveur.");
                return false;
            }

            run.ApplyRestResponse(response);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[STS-REST] Enchantement échoué : {ex.Message}");
            return false;
        }
    }

    private IEnumerator EnchantRoutine(int charges)
    {
        if (selectedCard == null || selectedController == null)
            yield break;

        if (RunManager.Instance.restCharges < charges)
            yield break;

        isEnchanting = true;
        enchantPanel.gameObject.SetActive(false);

        selectedController.SetSelected(false);

        // Sous autorité serveur, c'est lui qui tire le niveau et applique
        // l'enchantement : le faire aussi ici doublerait l'effet, et le tirage local
        // serait de toute façon écrasé à la prochaine resynchronisation.
        if (RunManager.Instance.IsServerAuthoritative)
        {
            Task<bool> enchantOnServer = EnchantOnServerAsync(charges);
            while (!enchantOnServer.IsCompleted)
                yield return null;

            if (!enchantOnServer.Result)
            {
                isEnchanting = false;
                UpdateChargesDisplay();
                yield break;
            }
        }
        else
        {
            RunManager.Instance.restCharges -= charges;
            int enchantLevel = UnityEngine.Random.Range(charges, charges*2);
            Debug.Log($"Enchanting card with level {enchantLevel} using {charges} charges.");
            EnchantManager.ApplyEnchant(selectedCard, enchantLevel);
        }

        // Refresh immediately so the player can see the applied enchant visuals before it exits.
        selectedController.RefreshView();
        //if (selectedController.view != null)
        //    selectedController.view.Flash();

        Canvas canvas = selectedController.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (canvasRect != null)
        {
            Vector2 startScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 endScreenPosition = new Vector2(Screen.width - enchantExitScreenMargin, Screen.height - enchantExitScreenMargin);

            GameObject animatedCardObject = new GameObject("RestEnchantCardClone", typeof(RectTransform), typeof(CanvasGroup));
            animatedCardObject.transform.SetParent(canvasRect, false);
            animatedCardObject.transform.SetAsLastSibling();

            RectTransform animatedRoot = animatedCardObject.GetComponent<RectTransform>();
            animatedRoot.anchorMin = new Vector2(0.5f, 0.5f);
            animatedRoot.anchorMax = new Vector2(0.5f, 0.5f);
            animatedRoot.pivot = new Vector2(0.5f, 0.5f);
            animatedRoot.sizeDelta = new Vector2(200f, 300f);

            GameObject animatedCardViewObject = Instantiate(selectedController.view.gameObject, animatedRoot);
            animatedCardViewObject.transform.SetAsLastSibling();
            RectTransform animatedCardViewRect = animatedCardViewObject.GetComponent<RectTransform>();
            if (animatedCardViewRect != null)
            {
                animatedCardViewRect.anchorMin = Vector2.zero;
                animatedCardViewRect.anchorMax = Vector2.one;
                animatedCardViewRect.offsetMin = Vector2.zero;
                animatedCardViewRect.offsetMax = Vector2.zero;
                animatedCardViewRect.localScale = Vector3.one;
            }

            CardView animatedCardView = animatedCardViewObject.GetComponent<CardView>();
            if (animatedCardView != null)
            {
                animatedCardView.SetCard(selectedCard);
            }

            CanvasGroup animatedGroup = animatedCardObject.GetComponent<CanvasGroup>();
            if (animatedGroup != null)
            {
                animatedGroup.alpha = 1f;
            }

            selectedController.SetVisualVisible(false);

            yield return StartCoroutine(selectedController.PlayEnchantExitAnimation(enchantPreviewDelay, enchantExitDuration, startScreenPosition, endScreenPosition, animatedRoot));
            Destroy(animatedCardObject);
        }
        else
        {
            selectedController.SetVisualVisible(false);
        }

        selectedCard = null;
        selectedController = null;
        BuildDeck();
        UpdateChargesDisplay();
        isEnchanting = false;
    }
    public async void ReturnToMap()
    {
        if (!completionGate.TryBegin())
        {
            return;
        }

        if (!await TryCompleteCurrentNodeAsync("rest"))
        {
            completionGate.Reset();
            return;
        }

        STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Map", "rest_return");
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
            Debug.Log($"[STS-RUN] CompleteNode request (rest) runId={RunManager.Instance.runId} nodeId={nodeId}");
            STSApiNodeCompleteResponse response = await STSApiClient.CompleteNodeAsync(RunManager.Instance.runId, nodeId, request);
            if (response != null && response.accepted)
            {
                Debug.Log($"[STS-RUN] CompleteNode response (rest) accepted=true runId={response.runId} currentNodeId={response.currentNodeId}");
                RunManager.Instance.ApplyNodeCompleteResponse(response);
                if (RunManager.Instance.currentNode != null)
                {
                    RunManager.Instance.currentNode.completed = true;
                }
                return true;
            }

            Debug.LogWarning("[STS-RUN] CompleteNode response (rest) was null or rejected. Staying in rest scene so completion can be retried.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-RUN] CompleteNode request (rest) failed: {ex.Message}");
            return false;
        }
    }

    void UpdateChargesDisplay()
    {
        foreach (Transform child in chargesContainer)
            Destroy(child.gameObject);
        for (int i = 0; i < RunManager.Instance.restCharges; i++)
        {
            Instantiate(chargePrefab, chargesContainer);
        }

        UILayoutHelper.RebuildAfterFrame(this, chargesContainer as RectTransform);
    }

    private IEnumerator RefreshDeckContainerAfterFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        RectTransform deckRect = deckContainer as RectTransform;
        if (deckRect == null)
            yield break;

        LayoutRebuilder.ForceRebuildLayoutImmediate(deckRect);
        Canvas.ForceUpdateCanvases();

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(deckRect, deckRect);
        Vector3 size = bounds.size;

        if (size.x > 0f)
            deckRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + deckContentPadding * 2f);

        if (size.y > 0f)
            deckRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y + deckContentPadding * 2f);

        Canvas.ForceUpdateCanvases();
    }
}
