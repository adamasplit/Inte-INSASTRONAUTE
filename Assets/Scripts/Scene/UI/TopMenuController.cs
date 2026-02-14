using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TopMenuController : MonoBehaviour
{
    [Header("Menu Button")]
    public Button menuButton;
    public RectTransform menuButtonRect;
    
    [Header("Menu Panel")]
    public RectTransform menuPanel;
    public CanvasGroup menuPanelGroup;
    
    [Header("Menu Items")]
    public List<MenuItemData> menuItems = new List<MenuItemData>();
    
    [Header("Screens")]
    public RectTransform screensContainer;
    public List<RectTransform> screens;
    
    [Header("Settings")]
    public float animationDuration = 0.3f;
    public Vector2 closedPanelOffset = new Vector2(300f, 0f);
    public Color selectedItemColor = new Color(0.2f, 0.6f, 1f);
    public Color unselectedItemColor = Color.white;
    
    private bool isMenuOpen = false;
    private bool isAnimating = false;
    private int currentScreenIndex = 0;
    private Dictionary<RectTransform, Vector3> originalItemPositions = new Dictionary<RectTransform, Vector3>();
    private Vector2 originalPanelPosition;
    
    [System.Serializable]
    public class MenuItemData
    {
        public string itemName;
        public Button button;
        public TMP_Text text;
        public Image background;
        public int screenIndex;
    }
    
    void Start()
    {
        InitializeMenu();
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ToggleMenu);
        }
        
        // Store original positions of menu items
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (menuItems[i].button != null)
            {
                var itemRect = menuItems[i].button.GetComponent<RectTransform>();
                originalItemPositions[itemRect] = itemRect.localPosition;
            }
        }
        
        // Setup menu item buttons
        for (int i = 0; i < menuItems.Count; i++)
        {
            int index = i;
            if (menuItems[i].button != null)
            {
                menuItems[i].button.onClick.AddListener(() => OnMenuItemClick(index));
            }
        }
        
        // Start with first screen
        GoToScreen(0);
    }
    
    void InitializeMenu()
    {
        // Start with menu closed
        if (menuPanel != null && menuPanelGroup != null)
        {
            // Store the original position from the editor
            originalPanelPosition = menuPanel.anchoredPosition;
            
            // Move to closed position
            menuPanel.anchoredPosition = originalPanelPosition + closedPanelOffset;
            menuPanelGroup.alpha = 0f;
            menuPanelGroup.interactable = false;
            menuPanelGroup.blocksRaycasts = false;
        }
    }
    
    public void ToggleMenu()
    {
        if (isAnimating) return;
        
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }
    
    void OpenMenu()
    {
        if (isAnimating || isMenuOpen) return;
        
        isAnimating = true;
        isMenuOpen = true;
        
        menuPanelGroup.interactable = true;
        menuPanelGroup.blocksRaycasts = true;
        
        // Animate button rotation
        LeanTween.cancel(menuButtonRect.gameObject);
        LeanTween.rotateZ(menuButtonRect.gameObject, 90f, animationDuration)
            .setEaseOutBack();
        
        // Slide in menu panel using anchoredPosition
        LeanTween.cancel(menuPanel.gameObject);
        LeanTween.value(menuPanel.gameObject, menuPanel.anchoredPosition, originalPanelPosition, animationDuration)
            .setOnUpdate((Vector2 pos) => {
                if (menuPanel != null)
                    menuPanel.anchoredPosition = pos;
            })
            .setEaseOutCubic();
        
        // Fade in menu panel
        LeanTween.alphaCanvas(menuPanelGroup, 1f, animationDuration)
            .setOnComplete(() => {
                isAnimating = false;
                ForceRebuildLayout();
            });
        
        // Stagger menu items fade in (without position animation to avoid layout conflict)
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (menuItems[i].button != null)
            {
                var itemGroup = menuItems[i].button.GetComponent<CanvasGroup>();
                
                if (itemGroup == null)
                {
                    itemGroup = menuItems[i].button.gameObject.AddComponent<CanvasGroup>();
                }
                
                itemGroup.alpha = 0f;
                
                float delay = i * 0.05f;
                
                // Animate fade in only
                LeanTween.cancel(menuItems[i].button.gameObject);
                LeanTween.alphaCanvas(itemGroup, 1f, 0.25f)
                    .setDelay(delay);
            }
        }
    }
    
    void CloseMenu()
    {
        if (isAnimating || !isMenuOpen) return;
        
        isAnimating = true;
        isMenuOpen = false;
        
        menuPanelGroup.interactable = false;
        menuPanelGroup.blocksRaycasts = false;
        
        // Animate button rotation back
        LeanTween.cancel(menuButtonRect.gameObject);
        LeanTween.rotateZ(menuButtonRect.gameObject, 0f, animationDuration)
            .setEaseOutBack();
        
        // Slide out menu panel using anchoredPosition
        LeanTween.cancel(menuPanel.gameObject);
        LeanTween.value(menuPanel.gameObject, menuPanel.anchoredPosition, originalPanelPosition + closedPanelOffset, animationDuration)
            .setOnUpdate((Vector2 pos) => {
                if (menuPanel != null)
                    menuPanel.anchoredPosition = pos;
            })
            .setEaseInCubic();
        
        // Fade out menu panel
        LeanTween.alphaCanvas(menuPanelGroup, 0f, animationDuration)
            .setOnComplete(() => {
                isAnimating = false;
            });
    }
    
    void OnMenuItemClick(int index)
    {
        if (index < 0 || index >= menuItems.Count) return;
        
        int screenIndex = menuItems[index].screenIndex;
        GoToScreen(screenIndex);
        
        // Update visual feedback
        UpdateMenuItemSelection(index);
        
        // Animate button press
        var buttonRect = menuItems[index].button.GetComponent<RectTransform>();
        LeanTween.cancel(buttonRect.gameObject);
        LeanTween.scale(buttonRect.gameObject, Vector3.one * 0.95f, 0.1f)
            .setEaseOutCubic()
            .setOnComplete(() => {
                LeanTween.scale(buttonRect.gameObject, Vector3.one, 0.15f)
                    .setEaseOutBack();
            });
        
        // Close menu after selection
        CloseMenu();
    }
    
    void UpdateMenuItemSelection(int selectedIndex)
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (menuItems[i].text != null)
            {
                Color targetColor = (i == selectedIndex) ? selectedItemColor : unselectedItemColor;
                AnimateTextColor(menuItems[i].text, targetColor, 0.2f);
            }
            
            if (menuItems[i].background != null)
            {
                float targetAlpha = (i == selectedIndex) ? 0.15f : 0f;
                Color bgColor = menuItems[i].background.color;
                bgColor.a = targetAlpha;
                
                LeanTween.value(menuItems[i].background.gameObject, 
                    menuItems[i].background.color.a, targetAlpha, 0.2f)
                    .setOnUpdate((float alpha) => {
                        Color c = menuItems[i].background.color;
                        c.a = alpha;
                        menuItems[i].background.color = c;
                    });
            }
        }
    }
    
    void AnimateTextColor(TMP_Text text, Color targetColor, float duration)
    {
        Color startColor = text.color;
        LeanTween.value(text.gameObject, 0f, 1f, duration)
            .setOnUpdate((float t) => {
                if (text != null)
                {
                    text.color = Color.Lerp(startColor, targetColor, t);
                }
            })
            .setEaseOutQuad();
    }
    
    public void GoToScreen(int index)
    {
        if (index < 0 || index >= screens.Count)
        {
            Debug.LogError("Index écran invalide");
            return;
        }
        
        Debug.Log("Changement vers l'écran : " + index);
        currentScreenIndex = index;
        
        // Animate screen transition
        if (screensContainer != null)
        {
            Vector2 targetPos = new Vector2(-screensContainer.rect.width * (screens[index].anchorMin.x), screensContainer.anchoredPosition.y);
            
            LeanTween.cancel(screensContainer.gameObject);
            LeanTween.value(screensContainer.gameObject, screensContainer.anchoredPosition, targetPos, 0.4f)
                .setOnUpdate((Vector2 pos) => {
                    if (screensContainer != null)
                        screensContainer.anchoredPosition = pos;
                })
                .setEaseOutCubic();
        }
        
        // Refresh controllers for the selected screen
        RefreshScreenControllers(index);
    }
    
    void RefreshScreenControllers(int index)
    {
        if (index < 0 || index >= screens.Count) return;
        
        var cardCollection = screens[index].GetComponentInChildren<CardCollectionController>(true);
        if (cardCollection != null)
        {
            cardCollection.RefreshCollection();
        }
        
        var leaderboardController = screens[index].GetComponentInChildren<LeaderboardController>(true);
        if (leaderboardController != null)
        {
            //leaderboardController.RefreshLeaderboard();
        }
        
        var packCollection = screens[index].GetComponentInChildren<PackCollectionController>(true);
        if (packCollection != null)
        {
            packCollection.RefreshCollection();
        }
        
        var shopController = screens[index].GetComponentInChildren<ShopRemoteLoader>(true);
        if (shopController != null)
        {
            shopController.UpdateShopFromRemote();
        }
    }
    
    // Public method to close menu from outside
    public void ForceCloseMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
    }
    
    // Force rebuild layout group to fix layout issues after animations
    void ForceRebuildLayout()
    {
        if (menuPanel != null)
        {
            var layoutGroups = menuPanel.GetComponentsInChildren<LayoutGroup>();
            foreach (var layoutGroup in layoutGroups)
            {
                if (layoutGroup != null && layoutGroup.enabled)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
                }
            }
        }
    }
}
