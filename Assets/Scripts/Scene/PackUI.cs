using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class PackUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] public Image packImage;
    [SerializeField] private TextMeshProUGUI packNameText;
    [SerializeField] private Image borderImage;
    public string packId;

    public void SetPackData(int number, PackData packData)
    {
        numberText.text = number.ToString();
        packImage.sprite = packData.packSprite;
        packNameText.text = packData.packName;
        packId = packData.packId;

        // Optionnel : couleur selon rareté
        borderImage.color = GetColorFromRarity(packData);
    }

    Color GetColorFromRarity(PackData pack)
    {
        return pack switch
        {
            _ when pack.name.Contains("Legendary") => new Color(1f, 0.8f, 0.2f),
            _ when pack.name.Contains("Epic") => new Color(0.6f, 0.2f, 0.8f),
            _ when pack.name.Contains("Rare") => new Color(0.2f, 0.6f, 1f),
            _ => Color.gray
        };
    }

    public void OnPackSelected()
    {
        Debug.Log($"Pack selected: {packNameText.text}");
        PackCollectionController controller = FindFirstObjectByType<PackCollectionController>();
        if (controller != null)
        {
            controller.selectedPackUI = this;
            controller.UpdateSelectedPackUI(this);
        }
        else
        {
            Debug.LogWarning("PackCollectionController not found in the scene.");
        }
    }

    public void OnPackConfirmed()
    {
        Debug.Log($"Pack confirmed: {packNameText.text}");
        PackData packData = FindFirstObjectByType<PackCollectionController>()
            .allPacks.FirstOrDefault(p => p.packId == packId);
        if (packData != null)
        {
            PackOpen.Instance.OpenPack(packData);
        }
        else
        {
            Debug.LogError($"PackData not found for packId: {packId}");
        }
    }
}
