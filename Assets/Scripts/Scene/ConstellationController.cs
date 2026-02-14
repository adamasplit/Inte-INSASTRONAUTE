using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ConstellationController : MonoBehaviour
{
    public StarController[] stars;
    public GameObject starPrefab;

    private TaskCompletionSource<bool> selectionTcs;
    private bool selectionLocked = false;
    public GameObject ScreenSizer;
    public RectTransform revealMask;
    public RevealWave revealWave;
    
    private RectTransform canvasRect => (RectTransform)ScreenSizer.transform.root.GetComponent<RectTransform>();


    private void Awake()
    {
        RectTransform rootRect = (RectTransform)ScreenSizer.transform;
        RectTransform canvasRect = rootRect.root.GetComponent<RectTransform>();

        rootRect.sizeDelta = canvasRect.rect.size;

        foreach (var star in stars)
            star.Init(this);
    }
    [Header("Constellation Settings")]
    public Transform starsRoot; // parent des étoiles
public float constellationRadius => GetScreenBasedRadius();
public float minDistanceBetweenStars = 1.2f;

public LineRenderer lineRenderer;
float GetScreenBasedRadius(float marginPercent = 0.15f)
{
    Camera cam = Camera.main;

    float screenHeightWorld = cam.orthographicSize * 800f;
    float screenWidthWorld = screenHeightWorld * cam.aspect/1.5f;

    float minDimension = Mathf.Min(screenWidthWorld, screenHeightWorld);

    // On garde une marge pour éviter les bords
    return minDimension * (0.5f - marginPercent);
}
public async Task UnmaskStars()
{
    revealMask.sizeDelta = Vector2.zero;

    float maxRadius = Mathf.Sqrt(
        canvasRect.rect.width * canvasRect.rect.width +
        canvasRect.rect.height * canvasRect.rect.height
    );
    float duration = 2f;
    float t = 0f;
    while (t < duration)
    {
        float size = EaseOutCubic(t / duration) * maxRadius * 2f;
        revealMask.sizeDelta = new Vector2(size, size);
        await Task.Yield();
        t += Time.deltaTime;
    }
}

public async Task AnimateAllLines(float totalDuration = 1f)
{
    if (stars == null || stars.Length < 2)
        return;

    // Ordonner les étoiles
    stars = stars
        .OrderBy(s => Mathf.Atan2(
            s.transform.localPosition.y,
            s.transform.localPosition.x))
        .ToArray();

    int segmentCount = stars.Length - 1;
    float segmentDuration = totalDuration / segmentCount;

    // Initialisation du LineRenderer
    lineRenderer.positionCount = 1;
    lineRenderer.SetPosition(0, stars[0].transform.localPosition);
    lineRenderer.enabled = true;

    // Dessin progressif segment par segment
    var a=stars[0].Pulse().ContinueWith(_ => stars[0].SetVisible(true));
    await DrawSegment(stars[0], stars[0], 1, segmentDuration);
    for (int i = 0; i < segmentCount; i++)
    {
        await DrawSegment(
            stars[i],
            stars[i + 1],
            i + 1,
            segmentDuration
        );
    }

}


public void HideStars()
{
    lineRenderer.enabled = false;
}
public async Task GenerateStars()
{
    // Nettoyage
    foreach (Transform child in starsRoot)
        Destroy(child.gameObject);

    var possiblePulls = PullManager.Instance.possiblePulls;
    if (possiblePulls == null || possiblePulls.Length == 0)
    {
        Debug.LogError("[Constellation] No possible pulls found");
        return;
    }

    int starCount = possiblePulls.Length;

    stars = new StarController[starCount];

    // Centre légèrement décalé pour éviter rigidité
    Vector2 centerOffset = Random.insideUnitCircle * 0.5f;

    for (int i = 0; i < starCount; i++)
    {
        Vector2 pos = GetValidStarPosition(
            existingPositions: new System.Collections.Generic.List<Vector2>(
                System.Array.ConvertAll(stars, s => s != null ? (Vector2)s.transform.localPosition : Vector2.positiveInfinity)
            ),
            radius: constellationRadius,
            minDistance: minDistanceBetweenStars
        ) + centerOffset;

        GameObject starGO = Instantiate(starPrefab, starsRoot);
        starGO.transform.localPosition = pos;

        var star = starGO.GetComponent<StarController>();
        star.Init(this);

        // Optionnel : info visuelle fake
        if (i>= possiblePulls.Length)
            Debug.LogError("[Constellation] possiblePulls index out of range at index " + i);
        star.SetPreviewPull(possiblePulls[i]);

        stars[i] = star;
    }
    var prw = revealWave.PlayRevealWave();
    await AnimateAllLines();
}

Vector2 GetValidStarPosition(
    List<Vector2> existingPositions,
    float radius,
    float minDistance,
    int maxAttempts = 30)
{
    for (int i = 0; i < maxAttempts; i++)
    {
        Vector2 candidate = Random.insideUnitCircle * radius;

        bool valid = true;
        foreach (var pos in existingPositions)
        {
            if (Vector2.Distance(candidate, pos) < minDistance)
            {
                valid = false;
                break;
            }
        }

        if (valid)
            return candidate;
    }

    // fallback (rare)
    return Random.insideUnitCircle * radius;
}


    public async Task WaitForStarSelection()
    {
        selectionLocked = false;
        selectionTcs = new TaskCompletionSource<bool>();

        foreach (var star in stars)
            star.SetInteractable(true);

        await selectionTcs.Task;
    }

    public async Task PlayRarityReveal(int rarity)
    {
        selectionLocked = true;

        foreach (var star in stars)
            star.OnOtherStarSelected();

        var selectedStar = GetSelectedStar();
        await selectedStar.PlayRarityAnimation();
    }

    public void OnStarSelected(StarController star)
    {
        if (selectionLocked) return;

        selectionLocked = true;

        foreach (var s in stars)
            s.SetInteractable(false);

        star.SetSelected(true);
        selectionTcs.TrySetResult(true);
    }

    public StarController GetSelectedStar()
    {
        foreach (var star in stars)
            if (star.IsSelected)
                return star;
        return null;
    }
    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
    }

    private async Task DrawSegment(
        StarController from,
        StarController to,
        int index,
        float duration)
    {
        float t = 0f;

        lineRenderer.positionCount = index + 1;

        to.SetVisible(false);

        // Lance le fade-in en parallèle
        var fadeTask = to.FadeIn(duration * 0.7f);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = EaseOutCubic(t);

            Vector3 current = Vector3.Lerp(
                from.transform.localPosition,
                to.transform.localPosition,
                eased
            );

            lineRenderer.SetPosition(index, current);
            await Task.Yield();
        }

        lineRenderer.SetPosition(index, to.transform.localPosition);

        var pulseTask = to.Pulse();
    }


}
