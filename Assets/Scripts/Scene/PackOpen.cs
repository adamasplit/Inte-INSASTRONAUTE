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

    private long initialPC = 0;
    private long targetPC = 0;
    private bool skipAll = false;
    private bool queuedSkip = false;
    private TaskCompletionSource<bool> skipSignal;
    private PackOpenPhase currentPhase= PackOpenPhase.None;
    private void SetPhase(PackOpenPhase phase)
{
    currentPhase = phase;
}

    // Call this to skip to the next card in the pack opening sequence
    public void SkipToNextCard()
    {
        if (skipSignal == null)
        {
            queuedSkip = true;
            Debug.LogWarning($"[PackOpen] SkipToNextCard queued (no active wait yet). phase={currentPhase}");
            if (currentPhase == PackOpenPhase.Summary)
            {
                FindFirstObjectByType<RollingCounter>().endAnimationInstant();
            }
            return;
        }

        Debug.LogWarning($"[PackOpen] SkipToNextCard accepted. phase={currentPhase}");
        skipSignal?.TrySetResult(true);
    }

    // Call this to skip the entire pack opening and reveal all cards at once
    public void SkipPackOpening()
    {
        skipAll = true;
        if (skipSignal == null)
        {
            queuedSkip = true;
            Debug.LogWarning($"[PackOpen] SkipPackOpening queued (no active wait yet). phase={currentPhase}");
            return;
        }

        Debug.LogWarning($"[PackOpen] SkipPackOpening accepted. phase={currentPhase}");
        skipSignal?.TrySetResult(true);
    }

    private bool ConsumeQueuedSkip(string context)
    {
        if (!queuedSkip)
            return false;

        queuedSkip = false;
        Debug.LogWarning($"[PackOpen] Consuming queued skip ({context}). phase={currentPhase}, skipAll={skipAll}");
        return true;
    }

    private async Task WaitForSecondsSafe(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            await Task.Yield();
            elapsed += Mathf.Max(Time.deltaTime, 0.001f);
        }
    }

    private async Task WaitForSkipSignal(string context, float warnAfterSeconds = 8f)
    {
        if (ConsumeQueuedSkip(context))
            return;

        skipSignal = new TaskCompletionSource<bool>();

        float elapsed = 0f;
        bool warned = false;
        while (!skipSignal.Task.IsCompleted)
        {
            await Task.Yield();
            elapsed += Mathf.Max(Time.deltaTime, 0.001f);

            if (!warned && elapsed >= warnAfterSeconds)
            {
                warned = true;
                Debug.LogWarning($"[PackOpen] Still waiting for skip input ({context}). phase={currentPhase}, skipAll={skipAll}");
            }
        }

        skipSignal = null;
    }

    private async Task<bool> WaitForRevealOrSkip(Task revealTask, int cardIndex, float warnAfterSeconds = 8f)
    {
        if (ConsumeQueuedSkip($"reveal start (index={cardIndex})"))
            return true;

        skipSignal = new TaskCompletionSource<bool>();

        float elapsed = 0f;
        bool warned = false;
        while (!revealTask.IsCompleted && !skipSignal.Task.IsCompleted)
        {
            await Task.Yield();
            elapsed += Mathf.Max(Time.deltaTime, 0.001f);

            if (!warned && elapsed >= warnAfterSeconds)
            {
                warned = true;
                Debug.LogWarning($"[PackOpen] Still waiting for reveal or skip (index={cardIndex}). phase={currentPhase}, skipAll={skipAll}");
            }
        }

        bool skipped = skipSignal.Task.IsCompleted;
        skipSignal = null;

        if (!skipped)
            await revealTask;

        return skipped;
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
        queuedSkip = false;
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
            cardUI.SetCardData(1, cardData.sprite);

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
        await WaitForSecondsSafe(0.6f);
        await WaitForSkipSignal("face-down cards reveal", 10f);
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            spawnedCards[i].gameObject.SetActive(false);
        }

        // Révélation une par une
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (skipAll)
                break;

            bool skipped = await WaitForRevealOrSkip(spawnedCards[i].Reveal(), i, 10f);
            if (skipped)
                spawnedCards[i].forceEndFlip();

            await WaitForSkipSignal($"card reveal continue (index={i})", 10f);
            spawnedCards[i].HideCard();
        }

        // Skip all → tout révéler
        {
            foreach (var card in spawnedCards)
            {
                card.RevealCard();
                card.endReveal();
                await WaitForSecondsSafe(0.02f);
            }
        }
        await WaitForSkipSignal("pack closing", 10f);
        SetPhase(PackOpenPhase.Summary);
        initialPC = PlayerProfileStore.PC;
        targetPC = PlayerProfileStore.PC+PlayerProfileStore.GetPCReward(pulledCards);
        await FindFirstObjectByType<RollingCounter>().AnimateFromTo(initialPC, targetPC);
        
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

        int maxIterations = 1000; // Safety counter for WebGL
        int iterations = 0;
        while (elapsed < duration && iterations < maxIterations)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.001f); // Ensure non-zero for WebGL
            elapsed += deltaTime;
            float t = elapsed / duration;
            float eased = EaseOutCubic(t);

            rt.anchoredPosition = Vector2.Lerp(start, target, eased);
            await Task.Yield();
            iterations++;
        }

        rt.anchoredPosition = target;
    }

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private async Task PlayConstellationPhase()
    {
        try
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
        catch (System.Exception ex)
        {
            Debug.LogError($"[PackOpen] PlayConstellationPhase exception: {ex}");
            throw;
        }
    }

}