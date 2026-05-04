using UnityEngine;
using System.Collections.Generic;
public class RestManager : MonoBehaviour
{
    public GenericPanel enchantPanel;
    public Transform deckContainer;
    public GameObject cardPrefab;
    public GameObject chargePrefab;
    public Transform chargesContainer;
    CardInstance selectedCard;

    void Start()
    {
        if (RunManager.Instance==null)
        {
            GameObject newRunManager = new GameObject("RunManager");
            newRunManager.AddComponent<RunManager>();
            RunManager.Instance.StartRun("",50,new List<Relic>{},false);
        }
        BuildDeck();
        UpdateChargesDisplay();
    }

    void BuildDeck()
    {
        foreach (Transform child in deckContainer)
            Destroy(child.gameObject);
        foreach (var card in RunManager.Instance.deck)
        {
            var obj = Instantiate(cardPrefab, deckContainer);

            var ctrl = obj.GetComponent<RestCardController>();
            ctrl.Init(card, this);
        }
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
        enchantPanel.Show("Enchant card", options);
    }

    public void OnEnchant(int charges)
    {
        if (RunManager.Instance.restCharges < charges)
            return;

        RunManager.Instance.restCharges -= charges;

        EnchantManager.ApplyEnchant(selectedCard, charges);
        BuildDeck();
        UpdateChargesDisplay();
    }
    public void ReturnToMap()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("STS_Map");
    }

    void UpdateChargesDisplay()
    {
        foreach (Transform child in chargesContainer)
            Destroy(child.gameObject);
        for (int i = 0; i < RunManager.Instance.restCharges; i++)
        {
            Instantiate(chargePrefab, chargesContainer);
        }
    }
}