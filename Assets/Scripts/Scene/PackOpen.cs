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

        await PlayerProfileStore.RemovePackAsync(packData.packId, 1);
        // Retirer 1 pack + générer les cartes
        foreach (Transform c in cardRevealAnchor)
            Destroy(c.gameObject);

        // Pour test : afficher les cartes générées
        foreach (CardData cardData in GetPulledCards(packData))
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

            // Add card to player collection
            if (cardData != null && !string.IsNullOrEmpty(cardData.cardId))
            {
                await PlayerProfileStore.AddCardAsync(cardData.cardId, 1);
            }
            Destroy(fx.gameObject);
            await Task.Delay(500); // Pause between reveals
            Destroy(cardUI.gameObject);
        }

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