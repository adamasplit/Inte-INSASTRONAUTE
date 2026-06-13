using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class STSLoadingScreen : MonoBehaviour
{
    public Image loadingImage;
    public TextMeshProUGUI loadingText;

    // Public API to set progress from external loaders
    public void SetProgress(float progress)
    {
        UpdateLoadingScreen(progress);
    }

    private System.Collections.IEnumerator SimulateLoading()
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.5f; // Simulate loading progress
            UpdateLoadingScreen(progress);
            yield return null;
        }
        // Loading complete
    }

    private void UpdateLoadingScreen(float progress)
    {
        if (loadingImage != null)
        {
            loadingImage.fillAmount = progress; // Update the fill amount of the loading image
        }
        if (loadingText != null)
        {
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%"; // Update the loading text
        }
    }

    public void HideLoadingScreen()
    {
        gameObject.SetActive(false); // Hide the loading screen
    }
}