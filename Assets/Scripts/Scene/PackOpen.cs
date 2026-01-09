using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
public class PackOpen : MonoBehaviour
{
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

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public async void OpenPack(PackData packData)
    {
        panel.SetActive(true);
        await OpenPackRoutine(packData);
    }

    private async Task OpenPackRoutine(PackData packData)
    {
        CardData[] pulledCards = GetPulledCards(packData);
        
        // Retirer 1 pack + générer les cartes
        foreach (Transform c in cardRevealAnchor)
            Destroy(c.gameObject);

        // Pour test : afficher les cartes générées
        foreach (CardData cardData in pulledCards)
        {
            var cardUI = Instantiate(cardRevealPrefab, cardRevealAnchor);
            cardUI.SetCardData(1, cardData.sprite, cardData.borderColor);
            var fx = Instantiate(particlePrefab, fxCanvas.transform);
            fx.transform.position = cardUI.transform.position;
            // Animate opacity from 0 to 255 over 1 second
            Image cg = cardUI.GetComponent<CardUI>().cardImage;

            Color color = cg.color;
            color.a = 0f;
            cg.color = color;
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsed / 1f);
                cg.color = color;
                await Task.Yield();
            }
            color.a = 1f;
            cg.color = color;

            await Task.Delay(500);
            // Fade out particle system by reducing start color alpha
            float fadeDuration = 0.5f;
            float fadeElapsed = 0f;
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                Color startColor = main.startColor.color;
                while (fadeElapsed < fadeDuration)
                {
                    fadeElapsed += Time.deltaTime;
                    float newAlpha = Mathf.Lerp(1f, 0f, fadeElapsed / fadeDuration);
                    main.startColor = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
                    await Task.Yield();
                }
                main.startColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            }
            Destroy(fx.gameObject);
            await Task.Delay(500); // Pause between reveals
            Destroy(cardUI.gameObject);
        }
        await PlayerProfileStore.RemovePackAsync(packData.packId, 1);
        
        await PlayerProfileStore.AddCards(pulledCards);
        panel.SetActive(false);
    }

    private CardData[] GetPulledCards(PackData packData)
    {
        CardData[] result= new CardData[packData.cardCount];
        for (int i = 0; i < packData.cardCount; i++)
        {
            // Sélectionner une carte aléatoire parmi les cartes possibles du pack
            float totalWeight = 0f;
            foreach (var entry in packData.possibleCards)
            {
                totalWeight += entry.weight;
            }
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            foreach (var entry in packData.possibleCards)
            {
                cumulativeWeight += entry.weight;
                if (randomValue <= cumulativeWeight)
                {
                    var cardData = FindFirstObjectByType<CardCollectionController>()
                        .allCards
                        .FirstOrDefault(c => c.cardId == entry.cardId);
                    result[i] = cardData;
                    break;
                }
            }
        }
        return result;
    }
}