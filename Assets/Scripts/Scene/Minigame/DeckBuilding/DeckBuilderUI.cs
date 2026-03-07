using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckBuilderUI : MonoBehaviour
{
    public static DeckBuilderUI Instance;

    [Header("Deck UI")]
    public Transform deckContainer;
    public CardUI cardPrefab;
    public CardCollectionController collectionController;

    [Header("Controls")]
    public Button playButton;
    public TextMeshProUGUI deckCountText;

    void Awake()
    {
        if (DeckManager.Instance == null)
            FindFirstObjectByType<DeckManager>()?.Init();
        if (playButton)
            playButton.interactable = DeckManager.Instance.IsDeckValid();
        Instance = this;
    }

    void OnEnable()
    {
        Refresh();
        GetComponent<CanvasGroup>().alpha = 1f; // Assurer que le CanvasGroup est visible
        GetComponent<CanvasGroup>().blocksRaycasts = true; // Permettre les interactions
        GetComponent<CanvasGroup>().interactable = true; // Permettre les interactions
    }

    void OnDisable()
    {
        DeckManager.Instance.SaveDeck();
        GetComponent<CanvasGroup>().alpha = 0f; // Rendre le CanvasGroup invisible
        GetComponent<CanvasGroup>().blocksRaycasts = false; // Empêcher les interactions
        GetComponent<CanvasGroup>().interactable = false; // Empêcher les interactions
    }

    public void Refresh()
    {
        Debug.Log("[DeckBuilderUI] Refreshing UI...");
        collectionController.RefreshCollection(true,false,false);
        // Nettoyage UI
        foreach (Transform child in deckContainer)
            Destroy(child.gameObject);
        
        if (DeckManager.Instance == null)
        {
            FindFirstObjectByType<DeckManager>()?.Init();
            if (DeckManager.Instance == null)
            {
                Debug.LogError("DeckManager.Instance is still null after Init");
                return;
            }
        }

        // Recréer le deck visuellement
        foreach (var card in DeckManager.Instance.deck)
        {
            var item = Instantiate(cardPrefab, deckContainer);

            item.SetCardData(
                1,                 // quantité = 1 dans le deck
                card.sprite,
                card,
                false,
                true
            );
        }

        // UI infos
        int count = DeckManager.Instance.deck.Count;
        int max = DeckManager.Instance.maxDeckSize;
        
        if (deckCountText)
            deckCountText.text = $"{count}/{max}";

        if (playButton)
            playButton.interactable = DeckManager.Instance.IsDeckValid();
    }
}