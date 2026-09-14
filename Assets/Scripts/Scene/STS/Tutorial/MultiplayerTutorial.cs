using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tutoriel à étapes des menus multijoueur : un texte, et un cadre qui assombrit tout sauf
/// l'élément dont on parle — le même rendu que le tutoriel de combat.
///
/// <para>Le tutoriel de combat vit tout câblé dans la scène STS_Combat. Ici, l'overlay est
/// construit à l'exécution sur le canvas du menu : il réutilise <see cref="STSTutorialHighlight"/>
/// et <see cref="TutorialNode"/>, sans rien demander à la scène.</para>
///
/// <para>Chaque étape attend une pression sur l'écran. Le capteur de pression couvre tout, trou
/// du cadre compris : pendant une explication, le joueur ne peut pas appuyer par mégarde sur le
/// bouton qu'on lui montre.</para>
/// </summary>
public class MultiplayerTutorial : MonoBehaviour
{
    public readonly struct Step
    {
        public readonly string Text;
        public readonly RectTransform[] Targets;

        /// <param name="targets">
        /// Les éléments à encadrer ensemble. Aucun : l'étape assombrit tout l'écran. Tous absents
        /// ou masqués : l'étape est sautée, plutôt que de parler d'un élément qu'on ne voit pas.
        /// </param>
        public Step(string text, params RectTransform[] targets)
        {
            Text = text;
            Targets = targets ?? Array.Empty<RectTransform>();
        }
    }

    private const float OverlayAlpha = 0.6f;
    private const float HighlightPadding = 12f;
    private const float TextBoxMargin = 90f;

    private Canvas canvas;
    private RectTransform root;
    private STSTutorialHighlight highlight;
    private GameObject fullDim;
    private RectTransform textBox;
    private TextMeshProUGUI bodyText;

    private TutorialNode current;
    private bool pressed;
    private Action onFinished;

    public bool IsPlaying => current != null || (root != null && root.gameObject.activeSelf);

    public static MultiplayerTutorial Create(Canvas canvas, TMP_FontAsset font)
    {
        if (canvas == null)
            return null;

        RectTransform root = CreateRect("MultiplayerTutorial", canvas.transform);
        Stretch(root);

        MultiplayerTutorial tutorial = root.gameObject.AddComponent<MultiplayerTutorial>();
        tutorial.canvas = canvas;
        tutorial.root = root;
        tutorial.Build(font);
        root.gameObject.SetActive(false);
        return tutorial;
    }

    private void Build(TMP_FontAsset font)
    {
        fullDim = CreateImage("Dim", root, new Color(0f, 0f, 0f, OverlayAlpha)).gameObject;
        Stretch((RectTransform)fullDim.transform);

        highlight = gameObject.AddComponent<STSTutorialHighlight>();
        highlight.top = CreateImage("DimTop", root, new Color(0f, 0f, 0f, OverlayAlpha)).rectTransform;
        highlight.bottom = CreateImage("DimBottom", root, new Color(0f, 0f, 0f, OverlayAlpha)).rectTransform;
        highlight.left = CreateImage("DimLeft", root, new Color(0f, 0f, 0f, OverlayAlpha)).rectTransform;
        highlight.right = CreateImage("DimRight", root, new Color(0f, 0f, 0f, OverlayAlpha)).rectTransform;
        highlight.Hide();

        // Transparent, mais cible des rayons : c'est lui qui reçoit la pression, où qu'elle tombe.
        Image catcher = CreateImage("TapCatcher", root, Color.clear);
        Stretch(catcher.rectTransform);
        Button catcherButton = catcher.gameObject.AddComponent<Button>();
        catcherButton.transition = Selectable.Transition.None;
        catcherButton.onClick.AddListener(() => pressed = true);

        Image box = CreateImage("TextBox", root, new Color(0.05f, 0.07f, 0.11f, 0.95f));
        // La boîte laisse passer la pression jusqu'au capteur : appuyer sur le texte avance aussi.
        box.raycastTarget = false;
        textBox = box.rectTransform;
        textBox.sizeDelta = new Vector2(980f, 380f);

        bodyText = CreateText("Text", textBox, font, 40f, Color.white);
        RectTransform bodyRect = bodyText.rectTransform;
        Stretch(bodyRect);
        bodyRect.offsetMin = new Vector2(36f, 84f);
        bodyRect.offsetMax = new Vector2(-36f, -28f);
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 24f;
        bodyText.fontSizeMax = 40f;
        bodyText.alignment = TextAlignmentOptions.TopLeft;

        TextMeshProUGUI hint = CreateText("Hint", textBox, font, 26f, new Color(0.75f, 0.8f, 0.9f, 0.9f));
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = Vector2.zero;
        hintRect.anchoredPosition = new Vector2(36f, 22f);
        hintRect.sizeDelta = new Vector2(560f, 48f);
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        hint.text = "Touchez l'écran pour continuer";

        Image skip = CreateImage("SkipButton", textBox, new Color(1f, 1f, 1f, 0.14f));
        RectTransform skipRect = skip.rectTransform;
        skipRect.anchorMin = skipRect.anchorMax = skipRect.pivot = new Vector2(1f, 0f);
        skipRect.anchoredPosition = new Vector2(-24f, 16f);
        skipRect.sizeDelta = new Vector2(200f, 64f);
        skip.gameObject.AddComponent<Button>().onClick.AddListener(Finish);

        TextMeshProUGUI skipLabel = CreateText("Label", skipRect, font, 30f, Color.white);
        Stretch(skipLabel.rectTransform);
        skipLabel.alignment = TextAlignmentOptions.Center;
        skipLabel.text = "Passer";
    }

    /// <summary>
    /// Joue les étapes dans l'ordre. <paramref name="finished"/> est appelé à la dernière étape
    /// comme sur « Passer » : dans les deux cas, le joueur a vu ce qu'il voulait voir.
    /// </summary>
    public void Play(IReadOnlyList<Step> steps, Action finished = null)
    {
        if (IsPlaying || steps == null)
            return;

        TutorialNode first = null;
        for (int i = steps.Count - 1; i >= 0; i--)
        {
            Step step = steps[i];
            if (string.IsNullOrWhiteSpace(step.Text) || (step.Targets.Length > 0 && !AnyUsable(step.Targets)))
                continue;

            TutorialNode following = first;
            first = new TutorialNode
            {
                text = step.Text,
                onStart = () =>
                {
                    pressed = false;
                    Focus(step.Targets);
                },
                condition = () => pressed,
                next = () => following
            };
        }

        if (first == null)
        {
            finished?.Invoke();
            return;
        }

        onFinished = finished;
        root.SetAsLastSibling();
        root.gameObject.SetActive(true);
        highlight.Hide();
        fullDim.SetActive(true);
        textBox.gameObject.SetActive(false);
        StartCoroutine(BeginAfterLayout(first));
    }

    /// Un panneau qu'on vient d'activer n'a pas encore sa taille : l'encadrer tout de suite
    /// dessinerait le cadre autour d'un rectangle vide. On attend une frame.
    private IEnumerator BeginAfterLayout(TutorialNode first)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        textBox.gameObject.SetActive(true);
        current = first;
        Enter(current);
    }

    private void Update()
    {
        if (current == null || current.condition == null || !current.condition())
            return;

        current.onComplete?.Invoke();
        TutorialNode next = current.next?.Invoke();
        if (next == null)
        {
            Finish();
            return;
        }

        current = next;
        Enter(current);
    }

    private void Enter(TutorialNode node)
    {
        // Une notification ouverte entre deux étapes ne doit pas passer par-dessus le tutoriel.
        root.SetAsLastSibling();
        bodyText.text = node.text;
        node.onStart?.Invoke();
    }

    private void Focus(RectTransform[] targets)
    {
        bool found = false;
        Rect union = default;
        foreach (RectTransform target in targets)
        {
            if (!IsUsable(target))
                continue;

            HighlightTarget area = HighlightTarget.FromRectTransform(target, canvas);
            if (!area.valid)
                continue;

            union = found
                ? Rect.MinMaxRect(
                    Mathf.Min(union.xMin, area.screenRect.xMin),
                    Mathf.Min(union.yMin, area.screenRect.yMin),
                    Mathf.Max(union.xMax, area.screenRect.xMax),
                    Mathf.Max(union.yMax, area.screenRect.yMax))
                : area.screenRect;
            found = true;
        }

        if (!found)
        {
            highlight.Hide();
            fullDim.SetActive(true);
            PlaceTextBox(atTop: false);
            return;
        }

        fullDim.SetActive(false);
        highlight.Highlight(union, HighlightPadding);
        // Le texte va du côté opposé à l'élément, pour ne jamais le recouvrir.
        PlaceTextBox(atTop: union.center.y < Screen.height * 0.5f);
    }

    private void PlaceTextBox(bool atTop)
    {
        Vector2 anchor = new(0.5f, atTop ? 1f : 0f);
        textBox.anchorMin = anchor;
        textBox.anchorMax = anchor;
        textBox.pivot = anchor;
        textBox.anchoredPosition = new Vector2(0f, atTop ? -TextBoxMargin : TextBoxMargin);
    }

    private void Finish()
    {
        StopAllCoroutines();
        current = null;
        pressed = false;
        highlight.Hide();
        root.gameObject.SetActive(false);

        Action callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    private static bool AnyUsable(RectTransform[] targets)
    {
        foreach (RectTransform target in targets)
        {
            if (IsUsable(target))
                return true;
        }
        return false;
    }

    private static bool IsUsable(RectTransform target) => target != null && target.gameObject.activeInHierarchy;

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.fontSize = size;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
