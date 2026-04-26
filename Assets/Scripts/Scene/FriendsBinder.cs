using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendsBinder : MonoBehaviour
{
    [Header("Actions")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button addFriendButton;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField friendPlayerIdInput;

    [Header("List")]
    [SerializeField] private Transform friendsListContainer;
    [SerializeField] private GameObject friendRowPrefab;
    [SerializeField] private TMP_Text friendsCountText;
    [SerializeField] private GameObject emptyState;

    private static NotificationSystem Notif => NotificationSystem.Instance;

    private void Start()
    {
        WireUI();
        _ = RefreshFriendsAsync();
    }

    private void OnDestroy()
    {
        UnwireUI();
    }

    private void WireUI()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(() => _ = RefreshFriendsAsync());
        if (addFriendButton != null) addFriendButton.onClick.AddListener(() => _ = AddFriendFromInputAsync());
        if (friendPlayerIdInput != null) friendPlayerIdInput.onSubmit.AddListener(OnFriendInputSubmitted);
    }

    private void UnwireUI()
    {
        if (refreshButton != null) refreshButton.onClick.RemoveAllListeners();
        if (addFriendButton != null) addFriendButton.onClick.RemoveAllListeners();
        if (friendPlayerIdInput != null) friendPlayerIdInput.onSubmit.RemoveAllListeners();
    }

    private void OnFriendInputSubmitted(string submittedValue)
    {
        _ = AddFriendFromInputAsync();
    }

    public async Task RefreshFriendsAsync()
    {
        try
        {
            SetButtonsInteractable(false);
            await PlayerProfileStore.LoadFriendsAsync();
            RebuildFriendsList(PlayerProfileStore.FRIENDS);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FriendsBinder] Refresh failed: {ex.Message}");
            Notif?.ShowNotification("Impossible de charger les amis pour le moment.");
        }
        finally
        {
            SetButtonsInteractable(true);
        }
    }

    public async Task AddFriendFromInputAsync()
    {
        string friendId = friendPlayerIdInput != null ? friendPlayerIdInput.text.Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(friendId))
        {
            Notif?.ShowNotification("Entre un Player ID valide.");
            return;
        }

        if (friendId == Unity.Services.Authentication.AuthenticationService.Instance.PlayerId)
        {
            Notif?.ShowNotification("Tu ne peux pas t'ajouter toi-meme.");
            return;
        }

        try
        {
            SetButtonsInteractable(false);
            bool added = await PlayerProfileStore.AddFriendAsync(friendId);
            if (!added)
            {
                Notif?.ShowNotification("Cet ami est deja dans ta liste.");
                return;
            }

            if (friendPlayerIdInput != null)
                friendPlayerIdInput.text = string.Empty;

            RebuildFriendsList(PlayerProfileStore.FRIENDS);
            Notif?.ShowNotification("Ami ajoute.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FriendsBinder] Add failed: {ex.Message}");
            Notif?.ShowNotification("Impossible d'ajouter cet ami.");
        }
        finally
        {
            SetButtonsInteractable(true);
        }
    }

    private async Task RemoveFriendAsync(string friendId)
    {
        try
        {
            bool removed = await PlayerProfileStore.RemoveFriendAsync(friendId);
            if (!removed)
            {
                Notif?.ShowNotification("Ami introuvable dans la liste.");
                return;
            }

            RebuildFriendsList(PlayerProfileStore.FRIENDS);
            Notif?.ShowNotification("Ami retire.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FriendsBinder] Remove failed: {ex.Message}");
            Notif?.ShowNotification("Impossible de retirer cet ami.");
        }
    }

    private void RebuildFriendsList(List<string> friends)
    {
        if (friendsListContainer == null)
            return;

        foreach (Transform child in friendsListContainer)
        {
            Destroy(child.gameObject);
        }

        friends ??= new List<string>();

        if (friendsCountText != null)
            friendsCountText.text = $"Amis: {friends.Count}";

        if (emptyState != null)
            emptyState.SetActive(friends.Count == 0);

        if (friendRowPrefab == null)
            return;

        for (int i = 0; i < friends.Count; i++)
        {
            string friendId = friends[i];
            GameObject row = Instantiate(friendRowPrefab, friendsListContainer);

            TMP_Text rowText = null;
            Transform playerIdTextTf = row.transform.Find("TXT_PlayerId");
            if (playerIdTextTf != null)
                rowText = playerIdTextTf.GetComponent<TMP_Text>();
            if (rowText == null)
                rowText = row.GetComponentInChildren<TMP_Text>(true);

            if (rowText != null)
                rowText.text = friendId;

            Button rowButton = null;
            Transform removeButtonTf = row.transform.Find("BTN_Remove");
            if (removeButtonTf != null)
                rowButton = removeButtonTf.GetComponent<Button>();
            if (rowButton == null)
                rowButton = row.GetComponentInChildren<Button>(true);

            if (rowButton != null)
            {
                rowButton.onClick.RemoveAllListeners();
                rowButton.onClick.AddListener(() => _ = RemoveFriendAsync(friendId));
            }
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (refreshButton != null) refreshButton.interactable = interactable;
        if (addFriendButton != null) addFriendButton.interactable = interactable;
    }
}
