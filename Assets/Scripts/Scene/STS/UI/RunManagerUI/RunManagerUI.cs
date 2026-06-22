using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
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
            Debug.Log("EventSystem not found in scene - creating one at runtime.");
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
                Debug.Log("GraphicRaycaster missing on Canvas - adding one so UI can receive clicks.");
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
        }
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
    }
    
    void ShowRelics()
    {
        Debug.Log("ShowRelics called");
        if (relicListPanel != null)
            relicListPanel.Show(RunManager.Instance.relics);
    }
    
    void ShowDeck()
    {
        if (deckGridPanel != null)
            deckGridPanel.Show(RunManager.Instance.deck,"Deck");
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
}