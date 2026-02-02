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
    public ParticleSystem ps;

    public void Init(ConstellationController c)
    {
        controller = c;
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
    if (ps != null)
    {
        var main = ps.main;
        main.startColor = color;
        ps.gameObject.SetActive(true);
        ps.Play();
    }

    await Task.Delay(1000);
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
    Color GetRarityColor(int rarity)
{
    return rarity switch
    {
        0 => Color.white,
        1 => new Color(0.6f, 0.7f, 1f),
        2 => new Color(1f, 0.85f, 0.2f),
        3 => Color.magenta,
        _ => Color.white
    };
}

    public async Task PlayRarityAnimation(int rarity)
    {
        Vector3 startPos = transform.localPosition;
    Vector3 targetPos = Vector3.zero; // centre de la constellation

    Image core = GetComponentInChildren<Image>();
    Image glow = transform.Find("Glow").GetComponent<Image>();

    Color baseColor = GetRarityColor(rarity);
    core.color = baseColor;

    float moveDuration = 0.8f;
    float pulseDuration = 0.6f;
    float explosionDelay = 0.15f;

    // 1️⃣ Déplacement vers le centre
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

    // 2️⃣ Pulse de brillance (suspense)
    t = 0f;
    while (t < pulseDuration)
    {
        t += Time.deltaTime;
        float pulse = Mathf.Sin(t * 10f) * 0.5f + 0.5f;
        float intensity = Mathf.Lerp(1.2f, 2f, pulse);

        SetGlow(glow, baseColor, intensity);
        transform.localScale = Vector3.one * (1f + pulse * 0.15f);

        await Task.Yield();
    }

    // 3️⃣ Explosion
    await PlayExplosion(baseColor);

    gameObject.SetActive(false);
    }
}
