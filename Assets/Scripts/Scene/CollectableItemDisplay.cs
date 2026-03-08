using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Composant à attacher sur le Prefab <c>CollectableItem</c>.
/// Affiche une récompense journalière (tokens, pack, etc.) dans l'UI du panneau Daily Reward.
/// 
/// Structure attendue du Prefab :
///   CollectableItem (root)
///   ├── Icon  (Image)
///   ├── Label (TMP_Text) — libellé principal
///   └── Amount (TMP_Text) — quantité (masquée si = 1)
/// </summary>
public class CollectableItemDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Image iconImage;

    [Header("Sprites par défaut (si iconName non renseigné)")]
    [SerializeField] private Sprite tokenSprite;
    [SerializeField] private Sprite packSprite;
    [SerializeField] private Sprite defaultSprite;

    /// <summary>
    /// Configure l'affichage à partir d'un <see cref="DailyRewardItem"/> Remote Config.
    /// </summary>
    public void SetItem(DailyRewardItem item)
    {
        if (item == null) return;

        if (labelText != null)
            labelText.text = string.IsNullOrEmpty(item.label) ? BuildDefaultLabel(item) : item.label;

        if (amountText != null)
        {
            amountText.text = item.amount > 1 ? $"x{item.amount}" : string.Empty;
            amountText.gameObject.SetActive(item.amount > 1);
        }

        if (iconImage != null)
            iconImage.sprite = ResolveSprite(item);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string BuildDefaultLabel(DailyRewardItem item)
    {
        return item.type switch
        {
            "TOKEN" => $"{item.amount} Token{(item.amount > 1 ? "s" : "")}",
            "PC"    => $"{item.amount} Point{(item.amount > 1 ? "s" : "")} de Collection",
            "PACK"  => $"{item.amount} Pack{(item.amount > 1 ? "s" : "")}",
            _       => item.type
        };
    }

    private Sprite ResolveSprite(DailyRewardItem item)
    {
        // 1. Tentative depuis Resources/Icons/ si iconName est défini
        if (!string.IsNullOrEmpty(item.iconName))
        {
            var loaded = Resources.Load<Sprite>($"Icons/{item.iconName}");
            if (loaded != null) return loaded;
        }

        // 2. Sprite de secours selon le type
        return item.type switch
        {
            "TOKEN" => tokenSprite != null ? tokenSprite : defaultSprite,
            "PACK"  => packSprite  != null ? packSprite  : defaultSprite,
            _       => defaultSprite
        };
    }
}
