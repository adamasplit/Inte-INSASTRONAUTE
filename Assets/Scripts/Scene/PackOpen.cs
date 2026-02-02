using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lean.Gui;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
enum PackOpenPhase
{
    None,
    Transit,
    Constellation,
    CardReveal,
    Summary
}

public class PackOpen : MonoBehaviour
{
    // State for skipping
    private bool skipToNext = false;
    private bool skipAll = false;
    private TaskCompletionSource<bool> skipSignal;
    private PackOpenPhase currentPhase= PackOpenPhase.None;
    private void SetPhase(PackOpenPhase phase)
{
    currentPhase = phase;
}

    // Call this to skip to the next card in the pack opening sequence
    public void SkipToNextCard()
    {
        skipToNext = true;
        skipSignal?.TrySetResult(true);
    }

    // Call this to skip the entire pack opening and reveal all cards at once
    public void SkipPackOpening()
    {
        skipAll = true;
        skipSignal?.TrySetResult(true);
    }

    public void StartPackOpenAnimation()
    {
        Debug.Log("Starting pack open animation...");
        this.gameObject.SetActive(true);
    }

    public static PackOpen Instance;
    [Header("UI")]
    public GameObject panel;
    public CardUI cardRevealPrefab;
    public Transform cardRevealAnchor;
    private CardData[] pulledCards;
    public GameObject particlePrefab;
    public Canvas fxCanvas;
    public Transform summaryGrid;
    public GameObject loadingScreen;
    public GameObject constellationRoot;
    private ConstellationController constellationController;
    public StarController ChosenStar => constellationController.GetSelectedStar();

    private void Awake()
    {
        Instance = this;
        constellationController = constellationRoot.GetComponent<ConstellationController>();
        OpenPack(PullManager.Instance.ChosenPack);
    }

    public async void OpenPack(PackData packData)
    {
        NavigationLock.IsScreenSwipeLocked = true;
        PullManager.Instance.GeneratePull(packData);
        panel.SetActive(true);
        skipToNext = false;
        skipAll = false;
        await OpenPackRoutine(packData);
        NavigationLock.IsScreenSwipeLocked = false;
    }

    private async Task OpenPackRoutine(PackData packData)
    {
        SetPhase(PackOpenPhase.Constellation);
        await PlayConstellationPhase();
        SetPhase(PackOpenPhase.CardReveal);
        Debug.Log("[PackOpen] Starting card reveal phase...");
        CardData[] pulledCards = ChosenStar.cards;
        if (pulledCards == null || pulledCards.Length == 0)
        {
            Debug.LogError("[PackOpen] No cards pulled!");
            return;
        }
        foreach (Transform c in summaryGrid)
                Destroy(c.gameObject);
        foreach (Transform c in cardRevealAnchor)
            Destroy(c.gameObject);

        // Reveal cards with skip logic
        for (int i = 0; i < pulledCards.Length; i++)
        {
            if (skipAll)
                break;

            var cardData = pulledCards[i];
            if (cardData == null)
            {
                Debug.LogError("[PackOpen] Pulled card data is null at index " + i);
                continue;
            }
            var cardUI = Instantiate(cardRevealPrefab, cardRevealAnchor);
            cardUI.SetCardData(1, cardData.sprite, cardData.borderColor);
            var fx = Instantiate(particlePrefab, fxCanvas.transform);
            fx.transform.position = cardUI.transform.position;
            Image cg = cardUI.GetComponent<CardUI>().cardImage;

            Color color = cg.color;
            color.a = 0f;
            cg.color = color;
            float elapsed = 0f;
            while (elapsed < 1f && !skipToNext && !skipAll)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsed / 1f);
                cg.color = color;
                await Task.Yield();
            }
            color.a = 1f;
            cg.color = color;


            // Fade out particle system by reducing start color alpha
            float fadeDuration = 0.5f;
            float fadeElapsed = 0f;
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                Color startColor = main.startColor.color;
                while (fadeElapsed < fadeDuration && !skipToNext && !skipAll)
                {
                    fadeElapsed += Time.deltaTime;
                    float newAlpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                    main.startColor = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
                    await Task.Yield();
                }
                main.startColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            }
            Destroy(fx.gameObject);

            // Wait for 500ms or skip
            skipSignal = new TaskCompletionSource<bool>();
            var completed = await Task.WhenAny(skipSignal.Task);
            skipSignal = null;
            skipToNext = false;

            Destroy(cardUI.gameObject);
        }

        // If skipping all, reveal all remaining cards instantly
        if (skipAll)
        {
            for (int i = 0; i < pulledCards.Length; i++)
            {
                var cardData = pulledCards[i];
                var cardUI = Instantiate(cardRevealPrefab, summaryGrid);
                cardUI.SetCardData(1, cardData.sprite, cardData.borderColor);
                CardReveal cardReveal = cardUI.GetComponent<CardReveal>();
                cardReveal.RevealCard();
                await Task.Delay(50);
            }
            skipSignal = new TaskCompletionSource<bool>();
            await Task.WhenAny(skipSignal.Task);
            skipSignal = null;
            skipToNext = false;
        }
        loadingScreen.SetActive(true);
        await PlayerProfileStore.RemovePackAsync(packData.packId, 1);
        await PlayerProfileStore.AddCards(pulledCards);
        loadingScreen.SetActive(false);
        panel.SetActive(false);
        SetPhase(PackOpenPhase.None);
        SceneManager.LoadScene("Main - Copie");
    }

    private async Task PlayConstellationPhase()
    {
        // Afficher la constellation
        constellationRoot.SetActive(true);
        constellationController.GenerateStars();
        // Attendre le choix du joueur
        await constellationController.WaitForStarSelection();
        // Récupérer la rareté
        var rarity = PullManager.Instance.highestRarity;

        // Jouer l’animation correspondante
        await constellationController.PlayRarityReveal(rarity);

        constellationRoot.SetActive(false);
    }

}