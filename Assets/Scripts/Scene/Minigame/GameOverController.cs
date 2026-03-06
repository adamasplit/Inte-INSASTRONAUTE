using UnityEngine;
using TMPro;
using System.Threading.Tasks;
public class GameOverController : MonoBehaviour
{
    public GameObject gameOverImage;
    public TextMeshProUGUI scoreText;
    public async Task ShowWithFade()
    {
        gameObject.SetActive(true);
        scoreText.text = $"{GameManager.Instance.score}";
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0;
        float duration = 1f; // Duration of the fade
        float elapsedTime = 0f;

        int maxIterations = 1000; // Safety counter for WebGL
        int iterations = 0;
        while (elapsedTime < duration && iterations < maxIterations)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.001f); // Ensure non-zero for WebGL
            elapsedTime += deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / duration);
            await Task.Yield();
            iterations++;
        }
        canvasGroup.alpha = 1;
    }
    public async Task HideWithFade()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        float duration = 1f; // Duration of the fade
        float elapsedTime = 0f;

        int maxIterations = 1000; // Safety counter for WebGL
        int iterations = 0;
        while (elapsedTime < duration && iterations < maxIterations)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.001f); // Ensure non-zero for WebGL
            elapsedTime += deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1 - (elapsedTime / duration));
            await Task.Yield();
            iterations++;
        }
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void StartHide()
    {
        _ = HideWithFade();
    }
}