using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;
    public Transform tooltipLayer;
    public GameObject tooltipPrefab;

    /// <summary>L'écart vertical entre deux infobulles d'un même groupe, en unités du calque.</summary>
    [SerializeField] private float stackSpacing = 8f;

    /// <summary>
    /// Où se pose le groupe d'infobulles en cours : la position demandée par la première.
    ///
    /// <para>Les suivantes ne choisissent plus la leur. Elles étaient posées à un décalage fixe
    /// sous la précédente, ce qui les faisait se chevaucher dès qu'une était un peu haute, et
    /// figeait l'ordre d'affichage sur l'ordre d'appel.</para>
    /// </summary>
    private Vector3 stackAnchor;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Affiche une infobulle, empilée sous celles que le même geste a déjà ouvertes.
    /// </summary>
    /// <param name="aboveOthers">
    /// À placer en tête du groupe plutôt qu'à la suite. C'est le cas de la description d'une
    /// carte : quand un statut en ouvre une à côté de la sienne, c'est elle qu'on lit d'abord.
    /// </param>
    public void ShowTooltip(string name, string description, Vector3 position, bool erasePrevious = true, bool aboveOthers = false)
    {
        if (erasePrevious)
        {
            foreach (Transform child in tooltipLayer)
            {
                Tooltip presentTooltip = child.GetComponent<Tooltip>();
                if (presentTooltip != null)
                    presentTooltip.Hide();
                else
                    Destroy(child.gameObject);
            }
            stackAnchor = position;
        }
        else if (!HasStackedTooltip())
        {
            stackAnchor = position;
        }

        GameObject obj = Instantiate(tooltipPrefab, tooltipLayer);
        Tooltip tooltip = obj.GetComponent<Tooltip>();
        tooltip.stacksAbove = aboveOthers;
        tooltip.SetTooltip(this, name, description);
        tooltip.transform.position = stackAnchor;
        StackTooltips();
    }

    public void HideTooltip()
    {
        if (tooltipLayer == null)
            return;

        foreach (Transform child in tooltipLayer)
        {
            if (child == null)
                continue;

            // Hide immediately to avoid one-frame lingering text during card exits.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private bool HasStackedTooltip()
    {
        if (tooltipLayer == null)
            return false;

        foreach (Transform child in tooltipLayer)
        {
            Tooltip tooltip = child != null ? child.GetComponent<Tooltip>() : null;
            if (tooltip != null && !tooltip.IsHiding)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Range le groupe verticalement depuis l'ancre : celles qui se disent prioritaires
    /// d'abord, les autres à la suite, chacune sous la précédente.
    ///
    /// <para>Les hauteurs se mesurent sur les coins en repère monde plutôt que sur le
    /// <c>RectTransform</c> : le pivot du prefab n'a pas à être connu d'ici, et une infobulle
    /// qui vient d'apparaître est mesurée à son échelle définitive et non à celle, réduite,
    /// de son animation d'entrée.</para>
    /// </summary>
    private void StackTooltips()
    {
        if (tooltipLayer == null)
            return;

        List<Tooltip> ordered = new();
        List<Tooltip> trailing = new();
        foreach (Transform child in tooltipLayer)
        {
            Tooltip tooltip = child != null ? child.GetComponent<Tooltip>() : null;
            if (tooltip == null || tooltip.IsHiding)
                continue;

            if (tooltip.stacksAbove)
                ordered.Add(tooltip);
            else
                trailing.Add(tooltip);
        }
        ordered.AddRange(trailing);

        float gap = stackSpacing * tooltipLayer.lossyScale.y;
        Vector3[] corners = new Vector3[4];
        bool first = true;
        float top = 0f;

        foreach (Tooltip tooltip in ordered)
        {
            RectTransform rect = tooltip.transform as RectTransform;
            if (rect == null)
                continue;

            Vector3 animatedScale = rect.localScale;
            rect.localScale = Vector3.one;
            rect.position = stackAnchor;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            rect.GetWorldCorners(corners);

            float height = corners[1].y - corners[0].y;
            if (first)
            {
                // La première reste exactement où l'appelant l'a demandée ; c'est elle qui
                // fixe le haut du groupe.
                top = corners[1].y;
                first = false;
            }
            else
            {
                rect.position += new Vector3(0f, top - corners[1].y, 0f);
            }

            top -= height + gap;
            rect.localScale = animatedScale;
        }
    }
}
