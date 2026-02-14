using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lean.Gui;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
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
        skipAll = false;
        await OpenPackRoutine(packData);
        NavigationLock.IsScreenSwipeLocked = false;
    }

    private async Task OpenPackRoutine(PackData packData)
    {
        SetPhase(PackOpenPhase.Constellation);
        await PlayConstellationPhase();
        SetPhase(PackOpenPhase.CardReveal);
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

        // ===============================
        // CARD REVEAL – JAILLISSEMENT + FLIP
        // ===============================

        List<CardReveal> spawnedCards = new();

        // Position centrale (UI)
        Vector2 explosionPos = Vector2.zero;

        // Spawn de toutes les cartes face cachée
        for (int i = 0; i < pulledCards.Length; i++)
        {
            var cardData = pulledCards[i];
            if (cardData == null) continue;

            var cardUI = Instantiate(cardRevealPrefab, cardRevealAnchor);
            cardUI.SetCardData(1, cardData.sprite, cardData.borderColor);

            var reveal = cardUI.GetComponent<CardReveal>();
            reveal.SetRarity(cardData.rarity);
            reveal.SetFaceDown();

            RectTransform rt = cardUI.GetComponent<RectTransform>();
            rt.anchoredPosition = explosionPos;
            rt.localScale = Vector3.one * 0.7f;

            spawnedCards.Add(reveal);
        }

        // Animation de jaillissement
        float radius = 420f;
        float startAngle = -90f;

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            float angle = startAngle + (360f / spawnedCards.Count) * i;
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            Vector2 targetPos = explosionPos + dir * radius;

            AnimateCardMove(
                spawnedCards[i].GetComponent<RectTransform>(),
                targetPos
            );
            spawnedCards[i].MemorizeFaceDown(targetPos);
        }


        // Petite pause après explosion
        await Task.Delay(600);
        skipSignal = new TaskCompletionSource<bool>();
        await Task.WhenAny(skipSignal.Task);
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            spawnedCards[i].gameObject.SetActive(false);
        }

        // Révélation une par une
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (skipAll)
                break;

            skipSignal = new TaskCompletionSource<bool>();

            await Task.WhenAny(spawnedCards[i].Reveal(),skipSignal.Task);
            if (skipSignal.Task.IsCompleted)
                spawnedCards[i].forceEndFlip();

            skipSignal = new TaskCompletionSource<bool>();
            await Task.WhenAny(
                skipSignal.Task
            );

            skipSignal = null;
            spawnedCards[i].HideCard();
        }

        // Skip all → tout révéler
        {
            foreach (var card in spawnedCards)
            {
                card.RevealCard();
                card.endReveal();
                await Task.Delay(20);
            }
        }


        // If skipping all, reveal all remaining cards instantly
        /*if (skipAll)
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
        }*/
        skipSignal = new TaskCompletionSource<bool>();
        await Task.WhenAny(skipSignal.Task);
        loadingScreen.SetActive(true);
        await PlayerProfileStore.RemovePackAsync(packData.packId, 1);
        await PlayerProfileStore.AddCards(pulledCards);
        loadingScreen.SetActive(false);
        panel.SetActive(false);
        SetPhase(PackOpenPhase.None);
        SceneManager.LoadScene("Main - Copie");
    }

    async void AnimateCardMove(RectTransform rt, Vector2 target)
    {
        Vector2 start = rt.anchoredPosition;
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float eased = EaseOutCubic(t);

            rt.anchoredPosition = Vector2.Lerp(start, target, eased);
            await Task.Yield();
        }

        rt.anchoredPosition = target;
    }

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private async Task PlayConstellationPhase()
    {
        // Afficher la constellation
        constellationRoot.SetActive(true);
        constellationRoot.GetComponent<Image>().enabled = true;
        await constellationController.GenerateStars();
        // Attendre le choix du joueur
        await constellationController.WaitForStarSelection();
        // Récupérer la rareté
        var rarity = PullManager.Instance.highestRarity;

        // Jouer l’animation correspondante
        await constellationController.PlayRarityReveal(rarity);
    }

}