using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MultiplayerDeckCardItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private CardView cardView;
    [SerializeField] private Toggle includeToggle;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TextMeshProUGUI lockedReasonText;
    [SerializeField] private GameObject selectedOverlay;
    [Header("Long Press Preview")]
    [SerializeField] private float longPressDuration = 0.5f;
    [SerializeField] private float longPressScale = 1.65f;

    private string cardKey;
    private Action<string, bool> onToggleChanged;
    private bool suppressToggleCallback;
    private bool pointerHeld;
    private bool previewing;
    private bool ignoreNextToggle;
    private float pointerDownTime;
    private Vector3 initialScale;

    public string CardKey => cardKey;

    private void Awake()
    {
        if (includeToggle != null)
        {
            includeToggle.onValueChanged.RemoveAllListeners();
            includeToggle.onValueChanged.AddListener(HandleToggleChanged);
        }

        WireLongPressRelays();
    }

    private void WireLongPressRelays()
    {
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.raycastTarget)
                continue;

            MultiplayerDeckCardLongPressRelay relay = graphic.GetComponent<MultiplayerDeckCardLongPressRelay>();
            if (relay == null)
                relay = graphic.gameObject.AddComponent<MultiplayerDeckCardLongPressRelay>();

            relay.SetOwner(this);
        }
    }

    public void Bind(
        STSCardData cardData,
        string uniqueKey,
        bool selected,
        bool interactable,
        string lockedReason,
        Action<string, bool> toggleChanged
    )
    {
        cardKey = uniqueKey;
        onToggleChanged = toggleChanged;

        if (cardView == null)
        {
            cardView = GetComponentInChildren<CardView>(true);
        }

        if (cardView != null && cardData != null)
        {
            cardView.gameObject.SetActive(true);
            cardView.SetCard(new CardInstance(cardData));
        }

        SetSelected(selected);

        if (includeToggle != null)
            includeToggle.interactable = interactable;

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!interactable);
        }

        if (lockedReasonText != null)
        {
            lockedReasonText.text = interactable ? string.Empty : lockedReason;
        }
    }

    public void SetSelected(bool selected)
    {
        if (includeToggle != null)
        {
            suppressToggleCallback = true;
            includeToggle.isOn = selected;
            suppressToggleCallback = false;
        }

        if (selectedOverlay != null)
            selectedOverlay.SetActive(selected);
    }

    private void Update()
    {
        if (!pointerHeld || previewing || Time.unscaledTime - pointerDownTime < longPressDuration)
            return;

        previewing = true;
        initialScale = transform.localScale;
        transform.SetAsLastSibling();
        transform.localScale = initialScale * longPressScale;
        cardView?.ShowCardTooltips(false, true, true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerHeld = true;
        pointerDownTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndLongPressPreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EndLongPressPreview();
    }

    private void OnDisable()
    {
        EndLongPressPreview();
    }

    private void EndLongPressPreview()
    {
        pointerHeld = false;
        if (!previewing)
            return;

        previewing = false;
        ignoreNextToggle = true;
        transform.localScale = initialScale;
        cardView?.Deselect();
    }

    private void HandleToggleChanged(bool value)
    {
        if (suppressToggleCallback)
            return;

        if (ignoreNextToggle)
        {
            ignoreNextToggle = false;
            SetSelected(selectedOverlay != null && selectedOverlay.activeSelf);
            return;
        }

        onToggleChanged?.Invoke(cardKey, value);
    }
}

public class MultiplayerDeckCardLongPressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private MultiplayerDeckCardItem owner;

    public void SetOwner(MultiplayerDeckCardItem item)
    {
        owner = item;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner?.OnPointerDown(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        owner?.OnPointerUp(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.OnPointerExit(eventData);
    }
}
