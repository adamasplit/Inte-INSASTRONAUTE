using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PackCollectionController : MonoBehaviour
{
    public Transform packContainer;
    public PackUI packPrefab;
    public PackUI selectedPackUI;
    public Transform selectedPackDisplay;

    [Header("Data")]
    public PackData[] allPacks;

    public void UpdateSelectedPackUI(PackUI packUI)
    {
        if (selectedPackUI != null)
        {
            // Additional logic to update other UI elements based on the selected pack can be added here.
            if (selectedPackDisplay != null)
            {
                selectedPackDisplay.GetComponent<PackUI>().SetPackData(
                    1, 
                    allPacks.FirstOrDefault(p => p.packId == packUI.packId)
                );
                selectedPackDisplay.gameObject.SetActive(true);
            }
        }
    }
    private void InitializeAllPacks()
    {

        Debug.Log("Initializing all packs from Resources...");
        // Load all PackData assets from Resources/Packs and assign to allPacks
        allPacks = Resources.LoadAll<PackData>("Packs");

    }
    private void OnEnable()
    {
        InitializeAllPacks();
        Debug.Log("Refreshing pack collection UI...");
        RefreshCollection();
        PlayerProfileStore.OnPackCollectionChanged += RefreshCollection;
    }

    private void OnDisable()
    {
        PlayerProfileStore.OnPackCollectionChanged -= RefreshCollection;
    }

    public void RefreshCollection()
    {
        Debug.Log("Updating pack collection UI...");
        foreach (Transform child in packContainer)
            Destroy(child.gameObject);
        selectedPackDisplay.gameObject.SetActive(false);

        foreach (var pack in allPacks)
        {
            if (PlayerProfileStore.PACK_COLLECTION == null)
            {
                Debug.LogWarning("PACK_COLLECTION is null in PlayerProfileStore. Cannot refresh pack collection.");
                return;
            }
            if (pack == null)
            {
                Debug.LogWarning("Encountered null PackData in allPacks array.");
                continue;
            }
            if (PlayerProfileStore.PACK_COLLECTION.TryGetValue(pack.packId, out int qty) && qty > 0)
            {
                var item = Instantiate(packPrefab, packContainer);
                item.SetPackData(qty, pack);
            }
        }
    }
}
