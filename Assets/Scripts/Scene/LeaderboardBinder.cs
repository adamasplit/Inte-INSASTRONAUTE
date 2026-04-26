using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardBinder : MonoBehaviour
{
    [SerializeField] private LeaderboardController leaderboardController;

    [Header("Actions")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button loadMoreButton;

    [Header("Filters")]
    [SerializeField] private TMP_Dropdown leaderboardTypeDropdown;
    [SerializeField] private Toggle friendsOnlyToggle;
    [SerializeField] private TMP_Dropdown departmentDropdown;
    [SerializeField] private TMP_Dropdown themeDropdown;

    [Header("Filter IDs (index-based)")]
    [SerializeField] private string[] leaderboardTypeIds;
    [SerializeField] private string[] departmentIds;
    [SerializeField] private string[] themeIds;

    [Header("Department Multi-Select (optional)")]
    [SerializeField] private Toggle[] departmentMultiSelectToggles;
    [SerializeField] private string[] departmentMultiSelectIds;

    private static NotificationSystem Notif => NotificationSystem.Instance;

    private void Start()
    {
        WireUI();
        _ = RefreshLeaderboardAsync();
    }

    private void OnDestroy()
    {
        UnwireUI();
    }

    private void WireUI()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(() => _ = RefreshLeaderboardAsync());
        if (loadMoreButton != null) loadMoreButton.onClick.AddListener(() => _ = LoadMoreAsync());
        if (leaderboardTypeDropdown != null) leaderboardTypeDropdown.onValueChanged.AddListener(_ => OnFiltersChanged());
        if (friendsOnlyToggle != null) friendsOnlyToggle.onValueChanged.AddListener(_ => OnFiltersChanged());
        if (departmentDropdown != null) departmentDropdown.onValueChanged.AddListener(_ => OnFiltersChanged());
        if (themeDropdown != null) themeDropdown.onValueChanged.AddListener(_ => OnFiltersChanged());

        if (departmentMultiSelectToggles != null)
        {
            foreach (var toggle in departmentMultiSelectToggles)
            {
                if (toggle != null)
                    toggle.onValueChanged.AddListener(_ => OnFiltersChanged());
            }
        }
    }

    private void UnwireUI()
    {
        if (refreshButton != null) refreshButton.onClick.RemoveAllListeners();
        if (loadMoreButton != null) loadMoreButton.onClick.RemoveAllListeners();
        if (leaderboardTypeDropdown != null) leaderboardTypeDropdown.onValueChanged.RemoveAllListeners();
        if (friendsOnlyToggle != null) friendsOnlyToggle.onValueChanged.RemoveAllListeners();
        if (departmentDropdown != null) departmentDropdown.onValueChanged.RemoveAllListeners();
        if (themeDropdown != null) themeDropdown.onValueChanged.RemoveAllListeners();

        if (departmentMultiSelectToggles != null)
        {
            foreach (var toggle in departmentMultiSelectToggles)
            {
                if (toggle != null)
                    toggle.onValueChanged.RemoveAllListeners();
            }
        }
    }

    private void OnFiltersChanged()
    {
        _ = RefreshLeaderboardAsync();
    }

    public async Task RefreshLeaderboardAsync()
    {
        if (leaderboardController == null)
        {
            Debug.LogError("[LeaderboardBinder] leaderboardController non assigne.");
            return;
        }

        SetButtonsInteractable(false);

        try
        {
            ApplyFiltersToController();
            await leaderboardController.RefreshLeaderboardAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LeaderboardBinder] Erreur rafraichissement : {ex.Message}");
            Notif?.ShowNotification("Impossible de charger le classement. Reessaie plus tard.");
        }
        finally
        {
            SetButtonsInteractable(true);
            RefreshLoadMoreButton();
        }
    }

    public async Task LoadMoreAsync()
    {
        if (leaderboardController == null || !leaderboardController.HasMore)
            return;

        if (loadMoreButton != null)
            loadMoreButton.interactable = false;

        try
        {
            await leaderboardController.LoadMoreAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LeaderboardBinder] Erreur chargement supplementaire : {ex.Message}");
            Notif?.ShowNotification("Impossible de charger plus d'entrees.");
        }
        finally
        {
            RefreshLoadMoreButton();
        }
    }

    private void ApplyFiltersToController()
    {
        leaderboardController.SetLeaderboardId(ReadDropdownId(leaderboardTypeDropdown, leaderboardTypeIds, "PTD"));
        leaderboardController.SetFriendsOnly(friendsOnlyToggle != null && friendsOnlyToggle.isOn);

        string departmentFilter = ReadDepartmentFilter();
        leaderboardController.SetDepartmentFilter(departmentFilter);
        leaderboardController.SetThemeFilter(ReadDropdownId(themeDropdown, themeIds, string.Empty));
    }

    private string ReadDepartmentFilter()
    {
        string multiSelectValue = ReadMultiToggleIds(departmentMultiSelectToggles, departmentMultiSelectIds);
        if (!string.IsNullOrWhiteSpace(multiSelectValue))
            return multiSelectValue;

        return ReadDropdownId(departmentDropdown, departmentIds, string.Empty);
    }

    private static string ReadDropdownId(TMP_Dropdown dropdown, string[] ids, string fallback)
    {
        if (dropdown == null)
            return fallback;

        int index = Mathf.Max(0, dropdown.value);
        if (ids == null || index >= ids.Length)
            return fallback;

        string value = ids[index];
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ReadMultiToggleIds(Toggle[] toggles, string[] ids)
    {
        if (toggles == null || ids == null)
            return string.Empty;

        int count = Mathf.Min(toggles.Length, ids.Length);
        if (count <= 0)
            return string.Empty;

        var selected = new System.Collections.Generic.List<string>(count);
        for (int i = 0; i < count; i++)
        {
            if (toggles[i] == null || !toggles[i].isOn)
                continue;

            string id = ids[i];
            if (!string.IsNullOrWhiteSpace(id))
                selected.Add(id.Trim());
        }

        return selected.Count == 0 ? string.Empty : string.Join(",", selected);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (refreshButton != null) refreshButton.interactable = interactable;
        if (loadMoreButton != null) loadMoreButton.interactable = interactable;
    }

    private void RefreshLoadMoreButton()
    {
        if (loadMoreButton == null || leaderboardController == null)
            return;

        loadMoreButton.gameObject.SetActive(leaderboardController.HasMore);
        loadMoreButton.interactable = leaderboardController.HasMore;
    }
}
