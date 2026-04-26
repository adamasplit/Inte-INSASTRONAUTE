using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public CombatManager combat;

    public TextMeshProUGUI energyText;

    public Transform playerRoot;
    public Transform enemyRoot;

    public GameObject characterUIPrefab;
    public GameObject enemyPrefab;
    public GameObject playerPrefab;

    List<CharacterUI> characterUIs = new();

    public Transform handPanel;
    public GameObject cardButtonPrefab;
    public HandLayoutController handLayout;
    public List<DropZone> allZones = new();

    public void Init(CombatManager cm)
    {
        combat = cm;
        InitCharacters();
    }
    public void InitCharacters()
    {
        characterUIs.Clear();
        allZones.Clear();
        foreach(Transform child in playerRoot)
            Destroy(child.gameObject);
        foreach(Transform child in enemyRoot)
            Destroy(child.gameObject);
        // PLAYER
        if (combat.player != null)
        {
            var playerZone = Instantiate(playerPrefab, playerRoot);
            var pUI = playerZone.GetComponent<CharacterUI>();
            pUI.SetCharacter(combat.player);

            var dz = playerZone.GetComponent<DropZone>();
            dz.Init(combat, combat.player, false);
            allZones.Add(dz);
            characterUIs.Add(pUI);
        }
        // ENEMIES
        foreach (var enemy in combat.enemies)
        {
            var zone = Instantiate(enemyPrefab, enemyRoot);

            var dz2 = zone.GetComponent<DropZone>();
            dz2.Init(combat, enemy, true);
            var eUI = zone.GetComponent<CharacterUI>();
            eUI.SetCharacter(enemy);
            characterUIs.Add(eUI);
            allZones.Add(dz2);
        }
    }
    public void RefreshUI()
    {
        foreach (var ui in characterUIs)
        {
            ui.Refresh();
        }
        energyText.text = combat.player != null ? "Energy: " + combat.player.resources.energy : "Energy: 0";
    
        RefreshHand();
    }

    

    void RefreshHand()
    {
        List<RectTransform> cards = new();

        foreach (Transform child in handPanel)
            Destroy(child.gameObject);

        foreach (var card in combat.deck.hand)
        {
            GameObject obj = Instantiate(cardButtonPrefab, handPanel);
            RectTransform rt = obj.GetComponent<RectTransform>();

            obj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text =
                card.data.cardName;
            obj.GetComponentInChildren<CardView>().SetCard(card);
            obj.GetComponentInChildren<CardView>().RefreshDescription();
            cards.Add(rt);
        }

        handLayout.Arrange(cards);
    }

    public void HighlightTargets(TargetingMode mode, Character hovered)
    {
        foreach (var zone in allZones)
        {
            bool shouldHighlight = false;

            switch (mode)
            {
                case TargetingMode.Enemy:
                    shouldHighlight = zone.target == hovered;
                    break;

                case TargetingMode.AllEnemies:
                    shouldHighlight = zone.target != combat.player&& hovered != null;
                    break;

                case TargetingMode.Player:
                    shouldHighlight = (zone.target == combat.player) && (hovered == combat.player);
                    break;

                case TargetingMode.AllCharacters:
                    shouldHighlight = (hovered!=null);
                    break;
                case TargetingMode.None:
                    shouldHighlight = false;
                    break;
            }

            zone.SetHighlight(shouldHighlight);
        }
    }
}