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

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / duration);
            await Task.Yield();
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

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1 - (elapsedTime / duration));
            await Task.Yield();
        }
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void StartHide()
    {
        _ = HideWithFade();
    }
}