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
    public GameObject unrestrictedRoot;

    [Header("Run End Unlocks")]
    public RunEndUnlockPanel runEndUnlockPanel;
    public GameObject hudContentRoot; // Optional: HUD elements to hide while the unlock panel is shown

    public Image redOrGreenOverlay;
    
    void Start()
    {
        if (relicsButton != null)
            relicsButton.onClick.AddListener(ShowRelics);
        if (deckButton != null)
            deckButton.onClick.AddListener(ShowDeck);
        if (saveAndReturnToMenuButton != null)
            saveAndReturnToMenuButton.onClick.AddListener(SaveAndReturnToMenu);
        
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
        if (saveAndReturnToMenuButton != null)
        {
            bool canSave = SceneManager.GetActiveScene().name == "STS_Map"
                && RunManager.Instance.map != null
                && RunManager.Instance.player != null;

            saveAndReturnToMenuButton.interactable = canSave;
        }
        floorText.text = $"Étage {RunManager.Instance.currentFloor}";
        actText.text = $"Acte {RunManager.Instance.act + 1}";
        hpText.text = $"PV : {RunManager.Instance.player.currentHP}/{RunManager.Instance.player.maxHP}";
        
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