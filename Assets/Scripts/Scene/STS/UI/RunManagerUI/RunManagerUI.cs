using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
public class RunManagerUI : MonoBehaviour
{
    public TextMeshProUGUI floorText;
    public TextMeshProUGUI actText;
    public TextMeshProUGUI hpText;
    
    [Header("Relics Button")]
    public Button relicsButton;
    public TextMeshProUGUI relicsCountText;
    
    [Header("Deck Button")]
    public Button deckButton;
    public TextMeshProUGUI deckCountText;
    
    [Header("Panels")]
    public RelicListPanel relicListPanel;
    public DeckGridPanel deckGridPanel;
    public Canvas canvas;

    [Header("Run Session")]
    public Button saveAndReturnToMenuButton;
    public TextMeshProUGUI saveAndReturnToMenuButtonLabel;
    public GameObject unrestrictedRoot;

    [Header("Run End Unlocks")]
    public RunEndUnlockPanel runEndUnlockPanel;
    public GameObject hudContentRoot; // Optional: HUD elements to hide while the unlock panel is shown

    public Image redOrGreenOverlay;

    // Non-null pendant un duel PvP : le combat s'approprie alors l'entete de run (voir
    // BeginPvpCombatOverride) au lieu d'afficher etage/acte/sauvegarde.
    private CombatManager pvpCombatOverride;
    private string defaultSaveButtonLabelText;

    void Start()
    {
        if (relicsButton != null)
            relicsButton.onClick.AddListener(ShowRelics);
        if (deckButton != null)
            deckButton.onClick.AddListener(ShowDeck);
        if (saveAndReturnToMenuButton != null)
            saveAndReturnToMenuButton.onClick.AddListener(OnSaveButtonPressed);
        if (saveAndReturnToMenuButtonLabel == null && saveAndReturnToMenuButton != null)
            saveAndReturnToMenuButtonLabel = saveAndReturnToMenuButton.GetComponentInChildren<TextMeshProUGUI>();
        if (saveAndReturnToMenuButtonLabel != null)
            defaultSaveButtonLabelText = saveAndReturnToMenuButtonLabel.text;
        
        // Ensure an EventSystem exists so UI can receive clicks
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Ensure parent Canvas has a GraphicRaycaster so buttons receive events
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var gr = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr == null)
            {
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
        }

        SyncUnrestrictedState();
    }

    
    void Update()
    {
        if (RunManager.Instance == null) return;

        // Pendant un duel, le combat pilote ces champs lui-meme (voir BeginPvpCombatOverride) :
        // les ecraser ici chaque frame effacerait l'avis de combat et le decompte de tour.
        bool pvpOverride = pvpCombatOverride != null;

        if (saveAndReturnToMenuButton != null && !pvpOverride)
        {
            bool canSave = SceneManager.GetActiveScene().name == "STS_Map"
                && RunManager.Instance.map != null
                && RunManager.Instance.player != null;

            saveAndReturnToMenuButton.interactable = canSave;
        }
        if (!pvpOverride)
        {
            floorText.text = $"Étage {RunManager.Instance.currentFloor}";
            actText.text = $"Acte {RunManager.Instance.act + 1}";
        }
        // Le joueur n'existe qu'une fois la run creee. Entre la demande de creation et la
        // reponse du serveur — le temps que les catalogues se chargent, plusieurs secondes —
        // l'entete est deja affichee et cette ligne levait une exception par frame.
        Player player = RunManager.Instance.player;
        if (player != null)
            hpText.text = $"PV : {player.currentHP}/{player.maxHP}";
        
        // Update button counts
        if (relicsCountText != null)
            relicsCountText.text = RunManager.Instance.relics.Count.ToString();
        if (deckCountText != null)
            deckCountText.text = RunManager.Instance.deck.Count.ToString();
        if (canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }

        SyncUnrestrictedState();
    }

    public void SetUnrestrictedMode(bool enabled)
    {
        if (unrestrictedRoot != null)
        {
            unrestrictedRoot.SetActive(enabled);
        }
    }

    private void SyncUnrestrictedState()
    {
        if (unrestrictedRoot == null || RunManager.Instance == null)
            return;

        unrestrictedRoot.SetActive(RunManager.Instance.unrestrictedMode);
    }
    
    void ShowRelics()
    {
        if (relicListPanel != null)
            relicListPanel.Show(RunManager.Instance.relics);
    }
    
    void ShowDeck()
    {
        if (deckGridPanel != null)
            deckGridPanel.Show(RunManager.Instance.deck,"Deck");
    }

    /// <summary>
    /// Un duel s'approprie l'entete de run : l'acte devient le decompte de tour, l'etage
    /// devient l'avis de combat, et ce bouton devient celui d'abandon. Le PvE ne passe
    /// jamais par ici, donc il ne voit rien changer.
    /// </summary>
    public void BeginPvpCombatOverride(CombatManager combat)
    {
        pvpCombatOverride = combat;
    }

    /// Rend l'entete de run a la carte : le duel est fini, sauve ou perdu.
    public void EndPvpCombatOverride()
    {
        pvpCombatOverride = null;

        if (floorText != null)
            floorText.gameObject.SetActive(true);
        if (actText != null)
        {
            actText.gameObject.SetActive(true);
            actText.color = Color.white;
        }
        if (saveAndReturnToMenuButtonLabel != null)
            saveAndReturnToMenuButtonLabel.text = defaultSaveButtonLabelText;
    }

    void OnSaveButtonPressed()
    {
        if (pvpCombatOverride != null)
        {
            pvpCombatOverride.RequestSurrender();
            return;
        }

        SaveAndReturnToMenu();
    }

    public void ShowUnlockedCardsPanel(List<STSCardData> unlockedCards, Action onClosed)
    {
        if (runEndUnlockPanel == null || unlockedCards == null || unlockedCards.Count == 0)
        {
            onClosed?.Invoke();
            return;
        }

        gameObject.SetActive(true);
        if (hudContentRoot != null)
            hudContentRoot.SetActive(false);

        runEndUnlockPanel.Show(unlockedCards, () =>
        {
            if (hudContentRoot != null)
                hudContentRoot.SetActive(true);
            onClosed?.Invoke();
        });
    }

    void SaveAndReturnToMenu()
    {
        if (RunManager.Instance == null)
            return;

        if (!RunManager.Instance.SaveRunState())
        {
            Debug.LogWarning("Save failed. Staying in the current run.");
            return;
        }

        RunManager.Instance.OnRunEnd(false);
        STSSceneLoader.Instance?.LoadScene("STS_Boot");
    }

    public void FlashRedOverlay(float duration = 0.5f)
    {
        StopAllCoroutines(); // Stop any existing overlay flashes
        StartCoroutine(FlashOverlay(Color.red, duration));
    }
    public void FlashGreenOverlay(float duration = 0.5f)
    {
        StopAllCoroutines(); // Stop any existing overlay flashes
        StartCoroutine(FlashOverlay(Color.green, duration));
    }
    public IEnumerator FlashOverlay(Color color, float duration)
    {
        if (redOrGreenOverlay == null)
            yield break;

        redOrGreenOverlay.color = new Color(color.r, color.g, color.b, 0.5f);
        redOrGreenOverlay.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(0.5f - (elapsed / duration)/2f);
            redOrGreenOverlay.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        redOrGreenOverlay.gameObject.SetActive(false);
    }
}