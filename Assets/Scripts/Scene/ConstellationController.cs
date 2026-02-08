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


    private void Awake()
    {
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

    float screenHeightWorld = cam.orthographicSize * 700f;
    float screenWidthWorld = screenHeightWorld * cam.aspect;

    float minDimension = Mathf.Min(screenWidthWorld, screenHeightWorld);

    // On garde une marge pour éviter les bords
    return minDimension * (0.5f - marginPercent);
}

void GenerateLines()
{
    if (stars == null || stars.Length < 2)
        return;
    stars = stars
    .OrderBy(s => Mathf.Atan2(
        s.transform.localPosition.y,
        s.transform.localPosition.x))
    .ToArray();

    // Ordre simple : nearest neighbor chain
    Vector3[] positions = new Vector3[stars.Length];

    for (int i = 0; i < stars.Length; i++)
        positions[i] = stars[i].transform.localPosition;

    lineRenderer.positionCount = positions.Length;
    lineRenderer.SetPositions(positions);
    lineRenderer.enabled = true;
}

public void HideStars()
{
    lineRenderer.enabled = false;
}
public void GenerateStars()
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
    GenerateLines();
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
}
