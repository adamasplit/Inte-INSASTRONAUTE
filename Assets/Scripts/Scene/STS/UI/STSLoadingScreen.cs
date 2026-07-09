using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class STSLoadingScreen : MonoBehaviour
{
    public Image loadingImage;
    public TextMeshProUGUI loadingText;

    // Public API to set progress from external loaders
    public void SetProgress(float progress)
    {
        UpdateLoadingScreen(progress);
    }

    private void UpdateLoadingScreen(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);

        if (loadingImage != null)
        {
            loadingImage.fillAmount = clampedProgress; // Update the fill amount of the loading image
        }
        if (loadingText != null)
        {
            loadingText.text = $"Loading... {Mathf.RoundToInt(clampedProgress * 100)}%"; // Update the loading text
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