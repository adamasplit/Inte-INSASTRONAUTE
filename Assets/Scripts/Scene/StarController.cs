using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading.Tasks;

public class StarController : MonoBehaviour, IPointerClickHandler
{
    public bool IsSelected { get; private set; }
    private ConstellationController controller;
    public CardData[] cards;
    public Collider2D coll;
    public ParticleSystem explosionPS;
    public ParticleSystem sparklePS;
    public ParticleSystem slightSparklePS;
    public ParticleSystem raysPS;
    public Image image;
    public Image glowImage;
    private float pulsePower = 5f;

    public void Init(ConstellationController c)
    {
        controller = c;
        var emission = slightSparklePS.emission;
        emission.rateOverTime = Random.Range(10, 40)/10f;
    }
    private int getHighestRarity()
    {
        int highest = 0;
        foreach (var card in cards)
        {
            if (card.rarity > highest)
                highest = card.rarity;
        }
        return highest;
    }
void SetGlow(Image glow, Color color, float intensity)
{
    glow.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(intensity * 0.5f));
    glow.transform.localScale = Vector3.one * (1f + intensity * 0.3f);
}
float EaseOutCubic(float t)
{
    return 1f - Mathf.Pow(1f - t, 3f);
}
async Task PlayExplosion(Color color)
{
    // Flash rapide
    float flashDuration = 0.1f;
    float t = 0f;

    Image glow = transform.Find("Glow").GetComponent<Image>();

    while (t < flashDuration)
    {
        t += Time.deltaTime;
        float v = Mathf.Lerp(2f, 0f, t / flashDuration);
        SetGlow(glow, color, v);
        await Task.Yield();
    }

    // Particules
    if (explosionPS != null)
    {
        var main = explosionPS.main;
        main.startColor = color;
        explosionPS.gameObject.SetActive(true);
        explosionPS.Play();
    }
    await Task.Delay(100);
    raysPS.gameObject.SetActive(false);
    controller.GetComponent<Image>().enabled = false;
    controller.HideStars();
    image.enabled = false;
    glowImage.enabled = false;
    await Task.Delay((int)(explosionPS.main.duration * 1000)-100);
}

public void SetPreviewPull(CardData[] pull)
{
    cards = pull;
}
    public void SetInteractable(bool value)
    {
        // activer / désactiver collider ou raycast
        coll.enabled = value;
    }

    public void SetSelected(bool value)
    {
        IsSelected = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.OnStarSelected(this);
    }

    public void OnOtherStarSelected()
    {
        if (IsSelected) return;
        // fade out / particles
        gameObject.SetActive(false);
    }

    private float getMoveTimeByRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: return 0.8f;
            case 1: return 0.8f;
            case 2: return 1f;
            case 3: return 1.2f;
            case 4: return 1.9f;
            default: return 1.0f;
        }
    }

    private float getPulsePowerByRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: return 1f;
            case 1: return 3f;
            case 2: return 4f;
            case 3: return 5f;
            case 4: return 10f;
            default: return 5f;
        }
    }

    private float getPulseTimeByRarity(int rarity)
    {
        switch (rarity)
        {
            case 0: return 0f;
            case 1: return 0.1f;
            case 2: return 0.3f;
            case 3: return 0.6f;
            case 4: return 1.4f;
            default: return 0.8f;
        }
    }

    public async Task PlayRarityAnimation()
    {
        Vector3 startPos = transform.localPosition;
    Vector3 targetPos = Vector3.zero; // centre de la constellation

    Image core = GetComponentInChildren<Image>();
    Image glow = transform.Find("Glow").GetComponent<Image>();

    int rarity = getHighestRarity();
    Color baseColor = CardDatabase.Instance.GetRarityColor(rarity);
    core.color = baseColor;

    float moveDuration = getMoveTimeByRarity(rarity);
    float pulseDuration = getPulseTimeByRarity(rarity);
    pulsePower = getPulsePowerByRarity(rarity); 
    slightSparklePS.gameObject.SetActive(false);
    sparklePS.gameObject.SetActive(true);
    var sparkleMain = sparklePS.main;
    sparkleMain.startSize = 0.5f + rarity * 0.2f;
    var col = sparklePS.colorOverLifetime;
    col.enabled = true;
    Gradient grad = new Gradient();
    grad.SetKeys(
        new GradientColorKey[] { new GradientColorKey(baseColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
    );
    col.color = new ParticleSystem.MinMaxGradient(grad);
    float t = 0f;
    while (t < moveDuration)
    {
        t += Time.deltaTime;
        float eased = EaseOutCubic(t / moveDuration);

        transform.localPosition = Vector3.Lerp(startPos, targetPos, eased);

        // Brillance qui augmente pendant le move
        float glowStrength = Mathf.Lerp(0.3f, 1.2f, eased);
        SetGlow(glow, baseColor, glowStrength);

        await Task.Yield();
    }

    transform.localPosition = targetPos;

    t = 0f;
    bool peakReached = false;
    int peakCount = 0;
    raysPS.gameObject.SetActive(true);
    while (t < pulseDuration)
    {
        var emission = raysPS.emission;
        float maxRayRate = rarity switch
        {
            0 => 0f,
            1 => 20f,
            2 => 40f,
            3 => 80f,
            4 => 150f,
            _ => 50f
        };
        float rayRate = Mathf.Lerp(0f, maxRayRate, t / pulseDuration);
        emission.rateOverTime = rayRate;
        
        t += Time.deltaTime;
        float pulse = Mathf.Sin(t * 10f) * 0.5f + 0.5f;
        float intensity = Mathf.Lerp(1.2f, pulsePower*(t/pulseDuration+1), pulse);

        SetGlow(glow, baseColor, intensity);
        transform.localScale = Vector3.one * (1f + pulse * 0.15f);
        if (!peakReached && pulse>0.9f)
        {
            peakReached = true;
            peakCount++;
            
        }
        await Task.Yield();
    }
    sparklePS.gameObject.SetActive(false);
    await PlayExplosion(baseColor);

    gameObject.SetActive(false);
    }
}
