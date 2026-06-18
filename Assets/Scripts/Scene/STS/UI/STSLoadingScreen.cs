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
        gameObject.SetActive(false); // Hide the loading screen
    }
}