using UnityEngine;
using System.Threading.Tasks;
using Lean.Gui;
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        StartMenu,
        Playing,
        GameOver
    }
    public int spawnInterval = 120;
    public static GameManager Instance;
    public int score;
    public int columnCount = 5;
    public int maxCardsInHand = 5;
    public GameState currentState= GameState.StartMenu;
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject bottomMenu;
    public GameObject gridManager;  
    public GameObject GameCardManager;
    public LeanDrag leanDrag;

    void Awake() => Instance = this;

    public void AddScore(int value)
    {
        score += value;
        //UIManager.Instance.UpdateScore(score);
        spawnInterval = Mathf.Max(30, spawnInterval - 2); // Decrease interval but not below 30
    }

    public void StartGame()
    {
        spawnInterval=120;
        score = 0;
        currentState = GameState.Playing;
        startPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        bottomMenu.SetActive(false);
        GameCardManager.GetComponent<GameCardManager>().Init();
        gridManager.SetActive(true);
        gridManager.GetComponent<GridManager>().LayoutColumns();
        leanDrag.gameObject.GetComponent<LeanConstrainAnchoredPosition>().HorizontalRectMax=-5;
    }

    public async Task GameOver()
    {
        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (score > best)
            PlayerPrefs.SetInt("BestScore", score);
        bottomMenu.SetActive(true);
        gridManager.SetActive(false);
        startPanel.SetActive(true);

        leanDrag.gameObject.GetComponent<LeanConstrainAnchoredPosition>().HorizontalRectMax=0;
        await GrantTokensOnGameOver();
    }

    public async Task GrantTokensOnGameOver()
    {
        int tokensToGrant = score / 10; // 1 token every 10 points
        foreach (var tokenUI in FindObjectsByType<UpdateDataUI>(FindObjectsSortMode.None))
        {
            tokenUI.RefreshDataUI();
            if (tokenUI.dataKey == "TOKEN")
            {
                tokenUI.alterDataUI(tokensToGrant);
            }
        }
        await CurrencyService.AddTokensAsync(tokensToGrant);
        
    }
}
