using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
public class GameOverController : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI reasonText;
    public CanvasGroup canvasGroup;
    void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    public void Show(Character enemy)
    {
        
        titleText.text = "Game over...";
        reasonText.text = $"Vous avez été vaincu par {enemy.name}!";
        StartCoroutine(FadeIn());
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    public IEnumerator FadeIn()
    {
        float duration = 1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        if (RunManager.Instance != null)RunManager.Instance.ui.gameObject.SetActive(false);
    }
    public void ToMenu()
    {
        SceneManager.LoadScene("STS_Boot");
    }
}