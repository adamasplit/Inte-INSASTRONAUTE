using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class STSLoadingScreen : MonoBehaviour
{
    public Image loadingImage;
    public TextMeshProUGUI loadingText;
    public Button cancelButton;
    [SerializeField] float fillLerpSpeed = 1.6f;

    private string defaultStatusText = "Chargement...";
    private Action cancelAction;
    private bool showCancelAction;
    private float displayedProgress;
    private float targetProgress;

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

    public void SetLoadingState(string statusText, bool canCancel = false, Action onCancel = null)
    {
        defaultStatusText = string.IsNullOrWhiteSpace(statusText) ? "Chargement..." : statusText;
        showCancelAction = canCancel;
        cancelAction = onCancel;
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(showCancelAction);
            cancelButton.onClick.RemoveAllListeners();
            if (showCancelAction)
            {
                cancelButton.onClick.AddListener(() => onCancel?.Invoke());
            }
        }
        RenderProgress(displayedProgress);
    }

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
            loadingImage.fillAmount = clampedProgress;
        }

        if (loadingText != null)
        {
            // Un écran qu'on peut quitter est une attente dont personne ne connaît la durée —
            // c'est la recherche d'adversaire. Le pourcentage y restait bloqué à 0 % et se
            // lisait comme une panne, alors qu'il n'y a simplement rien à mesurer.
            if (showCancelAction && !string.IsNullOrWhiteSpace(defaultStatusText))
            {
                loadingText.text = defaultStatusText;
                return;
            }

            int percentage = Mathf.RoundToInt(clampedProgress * 100f);
            loadingText.text = $"Chargement... {percentage}%";
        }
    }

    public void HideLoadingScreen()
    {
        if (!gameObject.activeSelf)
            return;
        StartCoroutine(HideLoadingScreenRoutine());
    }

    public IEnumerator HideLoadingScreenRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        gameObject.SetActive(false);
    }
}