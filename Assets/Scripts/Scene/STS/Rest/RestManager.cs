using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.UI;
public class RestManager : MonoBehaviour
{
    public GenericPanel enchantPanel;
    public Transform deckContainer;
    public GameObject cardPrefab;
    public GameObject chargePrefab;
    public Transform chargesContainer;
    CardInstance selectedCard;

    async void Start()
    {
        if (RunManager.Instance==null)
        {
            GameObject newRunManager = new GameObject("RunManager");
            newRunManager.AddComponent<RunManager>();
            await RunManager.Instance.StartRunAsync("",50,new List<Relic>{},false);
        }
        foreach (var relic in RunManager.Instance.relics)
        {
            relic.OnEnterRestSite(RunManager.Instance.player);
        }
        BuildDeck();
        UpdateChargesDisplay();
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
        while (RunManager.Instance.restCharges > 0&&RunManager.Instance.player.currentHP<RunManager.Instance.player.maxHP)
        {
            RunManager.Instance.player.Heal(Mathf.FloorToInt(RunManager.Instance.player.maxHP/6f));
            RunManager.Instance.restCharges--;
        }
        UpdateChargesDisplay();
    }

    // ---------------- ENCHANT ----------------
    public void OnCardSelected(CardInstance card)
    {
        if (selectedCard != null)
        {
            // Deselect the previously selected card
            var previousCardController = deckContainer.GetComponentInChildren<RestCardController>();
            if (previousCardController != null)
            {
                previousCardController.view.selectionHighlight.SetActive(false);
            }
        }
        selectedCard = card;

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
        if (RunManager.Instance.restCharges < charges)
            return;

        RunManager.Instance.restCharges -= charges;
        int enchantLevel = Random.Range(charges, charges*2+1);
        Debug.Log($"Enchanting card with level {enchantLevel} using {charges} charges.");
        EnchantManager.ApplyEnchant(selectedCard, enchantLevel);
        BuildDeck();
        UpdateChargesDisplay();
    }
    public void ReturnToMap()
    {
        STSSceneLoader.Instance.LoadScene("STS_Map");
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
            deckRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);

        if (size.y > 0f)
            deckRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

        Canvas.ForceUpdateCanvases();
    }
}