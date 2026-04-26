using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardElementPrefab;
    [SerializeField] private Transform contentParent;

    [Header("Optional Blocks")]
    [SerializeField] private GameObject separatorPrefab;
    [SerializeField] private GameObject loadMoreRowPrefab;

    [Header("Data")]
    [SerializeField] private string defaultLeaderboardId = "PTD";
    [SerializeField] private int pageSize = 20;
    [SerializeField] private bool submitScoreOnRefresh = true;
    [SerializeField] private bool useDummyData;

    [Header("Highlight Colors")]
    [SerializeField] private Color currentPlayerColor = new Color(0.2f, 0.6f, 1f, 1f);
    [SerializeField] private Color friendColor = new Color(0.95f, 0.8f, 0.25f, 1f);

    private string currentPlayerId;
    private string currentLeaderboardId;
    private bool friendsOnly;
    private string departmentFilter = string.Empty;
    private string themeFilter = string.Empty;
    private int nextOffset;
    private bool hasMore;
    private bool isLoadingMore;
    private GameObject loadMoreRowInstance;
    private Button loadMoreRowButton;
    private readonly HashSet<string> displayedPlayerIds = new HashSet<string>();
    private readonly HashSet<string> mainDisplayedPlayerIds = new HashSet<string>();
    private readonly HashSet<string> supplementalDisplayedPlayerIds = new HashSet<string>();
    private readonly List<GameObject> supplementalObjects = new List<GameObject>();
    private LeaderboardEntry cachedCurrentPlayer;
    private List<LeaderboardEntry> cachedFriendRows = new List<LeaderboardEntry>();

    public bool HasMore => hasMore;

    private void Awake()
    {
        currentPlayerId = AuthenticationService.Instance.PlayerId;
        currentLeaderboardId = string.IsNullOrWhiteSpace(defaultLeaderboardId)
            ? LeaderboardClient.DefaultLeaderboardId
            : defaultLeaderboardId;
    }

    public void SetLeaderboardId(string leaderboardId)
    {
        currentLeaderboardId = string.IsNullOrWhiteSpace(leaderboardId)
            ? LeaderboardClient.DefaultLeaderboardId
            : leaderboardId.Trim().ToUpperInvariant();
    }

    public void SetFriendsOnly(bool value)
    {
        friendsOnly = value;
    }

    public void SetDepartmentFilter(string value)
    {
        departmentFilter = NormalizeFilter(value);
    }

    public void SetThemeFilter(string value)
    {
        themeFilter = NormalizeFilter(value);
    }

    public async Task RefreshLeaderboardAsync()
    {
        if (useDummyData)
        {
            DummyPopulate();
            return;
        }

        ClearLeaderboard();
        nextOffset = 0;
        isLoadingMore = false;

        if (submitScoreOnRefresh)
        {
            await TrySubmitCurrentScoreAsync();
        }

        await LoadPageAsync(includeCurrentAndFriends: true);
        RefreshLoadMoreRow();
    }

    public async Task LoadMoreAsync()
    {
        if (useDummyData || !hasMore || isLoadingMore)
            return;

        isLoadingMore = true;
        RefreshLoadMoreRow();

        try
        {
            await LoadPageAsync(includeCurrentAndFriends: false);
        }
        finally
        {
            isLoadingMore = false;
            RefreshLoadMoreRow();
        }
    }

    private async Task LoadPageAsync(bool includeCurrentAndFriends)
    {
        var result = await LeaderboardClient.GetLeaderboardPageAsync(
            currentLeaderboardId,
            nextOffset,
            Mathf.Clamp(pageSize, 1, 50),
            friendsOnly,
            departmentFilter,
            themeFilter,
            includeCurrentPlayer: includeCurrentAndFriends,
            includeFriends: includeCurrentAndFriends
        );

        if (!result.ok)
        {
            Debug.LogError($"[Leaderboard] Failed to load leaderboard: {result.message}");
            hasMore = false;
            return;
        }

        ClearSupplementalSection();

        if (result.entries != null)
        {
            foreach (var entry in result.entries)
            {
                AddMainEntryIfNotDisplayed(entry);
            }
        }

        nextOffset = result.nextOffset;
        hasMore = result.hasMore;
        if (includeCurrentAndFriends)
        {
            cachedCurrentPlayer = result.currentPlayer;
            cachedFriendRows = result.friends ?? new List<LeaderboardEntry>();
        }

        RebuildSupplementalSection();
        RefreshLoadMoreRow();
    }

    private void RebuildSupplementalSection()
    {
        ClearSupplementalSection();

        var rowsToAppend = new List<LeaderboardEntry>();

        if (cachedCurrentPlayer != null &&
            !string.IsNullOrWhiteSpace(cachedCurrentPlayer.playerId) &&
            !mainDisplayedPlayerIds.Contains(cachedCurrentPlayer.playerId))
        {
            rowsToAppend.Add(cachedCurrentPlayer);
        }

        if (cachedFriendRows != null && cachedFriendRows.Count > 0)
        {
            var filteredFriends = cachedFriendRows
                .Where(f => f != null &&
                            !string.IsNullOrWhiteSpace(f.playerId) &&
                            !mainDisplayedPlayerIds.Contains(f.playerId))
                .GroupBy(f => f.playerId)
                .Select(g => g.OrderBy(x => x.rank).First())
                .OrderBy(f => f.rank)
                .ToList();

            rowsToAppend.AddRange(filteredFriends);
        }

        if (rowsToAppend.Count <= 0)
            return;

        supplementalObjects.Add(InstantiateSeparator());

        foreach (var row in rowsToAppend)
        {
            Color? bgColor = row.playerId == currentPlayerId ? currentPlayerColor : friendColor;
            var obj = AddEntry(row, bgColor);
            if (obj != null)
            {
                supplementalObjects.Add(obj);
                if (!string.IsNullOrWhiteSpace(row.playerId))
                    supplementalDisplayedPlayerIds.Add(row.playerId);
            }
        }
    }

    private void ClearSupplementalSection()
    {
        foreach (var playerId in supplementalDisplayedPlayerIds)
        {
            displayedPlayerIds.Remove(playerId);
        }

        supplementalDisplayedPlayerIds.Clear();

        foreach (var obj in supplementalObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        supplementalObjects.Clear();
    }

    private async Task TrySubmitCurrentScoreAsync()
    {
        if (!TryGetLocalScoreForLeaderboard(currentLeaderboardId, out long score))
            return;

        var submit = await LeaderboardClient.SubmitScoreAsync(currentLeaderboardId, score);
        if (!submit.ok)
        {
            Debug.LogWarning($"[Leaderboard] Submit failed: {submit.message}");
        }
    }

    private static bool TryGetLocalScoreForLeaderboard(string leaderboardId, out long score)
    {
        score = 0;

        switch ((leaderboardId ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "PC":
                score = PlayerProfileStore.PC;
                return true;
            default:
                return false;
        }
    }

    private static string NormalizeFilter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Equals("ALL", System.StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("TOUS", System.StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return trimmed;
    }

    private void AddMainEntryIfNotDisplayed(LeaderboardEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.playerId))
            return;

        if (displayedPlayerIds.Contains(entry.playerId))
            return;

        Color? bgColor = GetRankColor(entry.rank);

        if (entry.playerId == currentPlayerId)
        {
            bgColor = currentPlayerColor;
        }
        else if (entry.isFriend)
        {
            bgColor ??= friendColor;
        }

        AddEntry(entry, bgColor);
        mainDisplayedPlayerIds.Add(entry.playerId);
    }

    private GameObject AddEntry(LeaderboardEntry entry, Color? backgroundColor)
    {
        var obj = AddLeaderboardEntry(entry.rank, entry.displayName, (int)entry.score, null, backgroundColor);
        displayedPlayerIds.Add(entry.playerId);
        return obj;
    }

    private Color? GetRankColor(int rank)
    {
        return rank switch
        {
            1 => new Color(1f, 0.84f, 0f, 1f),
            2 => new Color(0.75f, 0.75f, 0.75f, 1f),
            3 => new Color(0.8f, 0.5f, 0.2f, 1f),
            _ => null
        };
    }

    private GameObject InstantiateSeparator()
    {
        if (separatorPrefab != null)
        {
            return Instantiate(separatorPrefab, contentParent);
        }

        var separator = new GameObject("Separator");
        separator.transform.SetParent(contentParent, false);

        var text = separator.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "• • •";
        text.fontSize = 24;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = Color.gray;

        var layout = separator.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredHeight = 40;

        return separator;
    }

    public void ClearLeaderboard()
    {
        displayedPlayerIds.Clear();
        mainDisplayedPlayerIds.Clear();
        supplementalDisplayedPlayerIds.Clear();
        supplementalObjects.Clear();
        cachedCurrentPlayer = null;
        cachedFriendRows = new List<LeaderboardEntry>();

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        loadMoreRowInstance = null;
        loadMoreRowButton = null;
    }

    public GameObject AddLeaderboardEntry(int rank, string playerName, int score, Sprite userIcon = null, Color? backgroundColor = null)
    {
        var newEntry = Instantiate(leaderboardElementPrefab, contentParent);
        var element = newEntry.GetComponent<LeaderboardElement>();
        element.SetData(rank, playerName, score, userIcon, backgroundColor);
        return newEntry;
    }

    public void DummyPopulate()
    {
        ClearLeaderboard();
        AddLeaderboardEntry(1, "Adamasploots", 1500, null, GetRankColor(1));
        AddLeaderboardEntry(2, "Offieks", 1200, null, GetRankColor(2));
        AddLeaderboardEntry(3, "Loris", 1000, null, GetRankColor(3));
        AddLeaderboardEntry(4, "Fhystel", 800);
        AddLeaderboardEntry(5, "Maitr", 600);
        InstantiateSeparator();
        AddLeaderboardEntry(9, "Ami_1", 220, null, friendColor);
        AddLeaderboardEntry(42, "Vous", 15, null, currentPlayerColor);
        hasMore = true;
        RefreshLoadMoreRow();
    }

    private void RefreshLoadMoreRow()
    {
        if (contentParent == null || loadMoreRowPrefab == null)
            return;

        if (!hasMore)
        {
            if (loadMoreRowInstance != null)
            {
                Destroy(loadMoreRowInstance);
                loadMoreRowInstance = null;
                loadMoreRowButton = null;
            }
            return;
        }

        if (loadMoreRowInstance == null)
        {
            loadMoreRowInstance = Instantiate(loadMoreRowPrefab, contentParent);
            loadMoreRowButton = loadMoreRowInstance.GetComponentInChildren<Button>(true);

            if (loadMoreRowButton != null)
            {
                loadMoreRowButton.onClick.RemoveAllListeners();
                loadMoreRowButton.onClick.AddListener(OnLoadMoreRowClicked);
            }
            else
            {
                Debug.LogWarning("[Leaderboard] loadMoreRowPrefab ne contient pas de Button.");
            }
        }

        loadMoreRowInstance.transform.SetAsLastSibling();
        if (loadMoreRowButton != null)
            loadMoreRowButton.interactable = !isLoadingMore;
    }

    private async void OnLoadMoreRowClicked()
    {
        await LoadMoreAsync();
    }
}
