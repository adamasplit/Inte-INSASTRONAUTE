using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class STSLoadingScreen : MonoBehaviour
{
    public Image loadingImage;
    public TextMeshProUGUI loadingText;
    [SerializeField] float fillLerpSpeed = 1.6f;

    float displayedProgress;
    float targetProgress;

    void OnEnable()
    {
        displayedProgress = 0f;
        targetProgress = 0f;
        RenderProgress(0f);
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (displayedProgress >= targetProgress)
            return;

        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, fillLerpSpeed * Time.unscaledDeltaTime);
        RenderProgress(displayedProgress);
    }

    // Public API to set progress from external loaders
    public void SetProgress(float progress)
    {
        targetProgress = Mathf.Max(targetProgress, Mathf.Clamp01(progress));
        if (targetProgress > displayedProgress)
        {
            RenderProgress(displayedProgress);
        }
    }

    private void RenderProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);

        if (loadingImage != null)
        {
            loadingImage.fillAmount = clampedProgress; // Update the fill amount of the loading image
        }
        if (loadingText != null)
        {
            loadingText.text = $"Chargement... {Mathf.RoundToInt(clampedProgress * 100)}%"; // Update the loading text
        }
    }

    public void HideLoadingScreen()
    {
        if (!gameObject.activeSelf)
            return; // Already hidden
        StartCoroutine(HideLoadingScreenRoutine());
    }
    public IEnumerator HideLoadingScreenRoutine()
    {
        // Optionally, you can add a fade-out effect here
        yield return new WaitForSeconds(0.2f); // Wait for half a second before hiding
        gameObject.SetActive(false);
    }
}