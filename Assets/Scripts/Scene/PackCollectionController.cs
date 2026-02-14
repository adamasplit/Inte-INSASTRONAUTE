using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;

public class PackCollectionController : MonoBehaviour
{
    public Transform packContainer;
    public PackUI packPrefab;
    public PackUI selectedPackUI;
    public RectTransform selectedPackDisplay;

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
    [Header("Animation")]
    public Image background;
    public CanvasGroup starFieldGroup;
    public Image flashImage;
    public GameObject bottomMenu;

    private float duration = 2f;
    private float shakeIntensity = 10f;
    private float shakeSpeed = 40f;
    public ParticleSystem speedParticles;


    public IEnumerator OpenPackAnimation()
    {
        bottomMenu.SetActive(false);
        float t = 0f;

        Vector3 startPos = selectedPackDisplay.anchoredPosition;
        Vector3 endPos = Vector3.zero; // centre écran

        Vector3 startScale = selectedPackDisplay.localScale;
        Vector3 endScale = Vector3.zero;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, t / duration);
            float d = Mathf.Max(0,t / duration-0.8f);
            // Le pack monte vers le centre
            selectedPackDisplay.anchoredPosition = Vector3.Lerp(startPos, endPos, p);

            // Le pack rétrécit
            selectedPackDisplay.localScale = Vector3.Lerp(startScale, endScale, p);

            // Le fond devient noir
            background.color = new Color(0, 0, 0, p);

            // Les étoiles apparaissent
            starFieldGroup.alpha = p;
            float currentShake = shakeIntensity * p;

            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * (currentShake);
            float shakeY = (Mathf.Cos(Time.time * shakeSpeed * 1.3f)+p*p*60f) * (currentShake);

            selectedPackDisplay.anchoredPosition += new Vector2(shakeX, shakeY);
            var emission = speedParticles.emission;
            // L'intensité des particules augmente jusqu'à la moitié de l'animation, puis diminue progressivement
            if (t/duration < 0.5f)
            emission.rateOverTime = p * 400f; // Augmente l'intensité des particules
            else
            emission.rateOverTime = (0.2f-d) * 400f; // Diminue l'intensité des particules
            yield return null;

        }

        selectedPackDisplay.gameObject.SetActive(false);
        StartCoroutine(Flash());
        
    }

    IEnumerator Flash()
{
    float t = 0f;
    flashImage.color = new Color(1,1,1,0);
    flashImage.transform.rotation = Quaternion.identity; // Réinitialise la rotation
    flashImage.transform.localScale = Vector3.one; // Réinitialise l'échelle
    flashImage.GetComponent<RectTransform>().position = selectedPackDisplay.position; // Positionne le flash sur le pack
    flashImage.gameObject.SetActive(true);

    while (t < 0.8f)
    {
        t += Time.deltaTime;
        if (t < 0.3f)
        {
            flashImage.color = new Color(1,1,1, t / 0.3f);
        }
        else
        {
            flashImage.color = new Color(1,1,1, 1 - (t - 0.3f) / 0.5f);
        }
        // Rotation ralentissant progressivement
        float rotationAngle = Mathf.Lerp(360, 0, t / 0.8f); // Commence à 360° et ralentit jusqu'à 0° 
        flashImage.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, rotationAngle); // Rotation rapide pendant le flash
        flashImage.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, t / 0.3f); // Agrandissement pendant le flash
        yield return null;
    }

    flashImage.gameObject.SetActive(false);
    SceneManager.LoadScene("PullScene");
}


}
