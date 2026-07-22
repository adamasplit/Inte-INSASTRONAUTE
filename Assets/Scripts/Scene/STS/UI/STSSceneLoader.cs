using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class STSSceneLoader : MonoBehaviour
{
    public static STSSceneLoader Instance { get; private set; }
    public STSLoadingScreen loadingScreen;
    private int backgroundLoadingCount = 0;
    private bool sceneTransitionPending = false;
    private float backgroundProgress = 0f;
    private float sceneTransitionProgress = 0f;
    private float sceneStartProgress = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep the scene loader across scenes
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }
    }

    public void BeginLoading()
    {
        if (backgroundLoadingCount == 0)
        {
            backgroundProgress = 0f;
        }

        backgroundLoadingCount++;
        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
            ApplyProgressToScreen();
        }
    }

    public void SetBackgroundProgress(float progress)
    {
        // Keep loading progression monotonic to avoid visible regressions.
        backgroundProgress = Mathf.Max(backgroundProgress, Mathf.Clamp01(progress));
        ApplyProgressToScreen();
    }

    public void LoadScene(string sceneName)
    {
        // Start async loading and update the loading screen progress.
        sceneTransitionPending = true;
        sceneTransitionProgress = 0f;
        sceneStartProgress = backgroundLoadingCount > 0
            ? Mathf.Clamp01(backgroundProgress)
            : 0f;

        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
            ApplyProgressToScreen();
        }
        StartCoroutine(LoadSceneAsyncRoutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        while (!op.isDone)
        {
            // op.progress is 0..0.9 while loading, then becomes 1 when done
            float progress = op.progress;
            sceneTransitionProgress = Mathf.Clamp01(progress / 0.9f);
            ApplyProgressToScreen();
            yield return null;
        }

        // Ensure progress reaches 100%
        sceneTransitionProgress = 1f;
        ApplyProgressToScreen();
    }

    public void EndLoading()
    {
        if (backgroundLoadingCount > 0)
        {
            backgroundLoadingCount--;
        }

        if (backgroundLoadingCount == 0 && !sceneTransitionPending)
        {
            backgroundProgress = 1f;
            ApplyProgressToScreen();
        }

        TryHideLoadingScreen();
    }

    public void SceneReady()
    {
        sceneTransitionPending = false;
        sceneTransitionProgress = 1f;
        ApplyProgressToScreen();
        TryHideLoadingScreen();
    }

    private float GetCurrentProgress()
    {
        if (sceneTransitionPending)
        {
            return Mathf.Lerp(sceneStartProgress, 1f, sceneTransitionProgress);
        }

        if (backgroundLoadingCount > 0)
        {
            return backgroundProgress;
        }

        return 1f;
    }

    private void ApplyProgressToScreen()
    {
        if (loadingScreen == null)
        {
            return;
        }

        loadingScreen.SetProgress(GetCurrentProgress());
    }

    private void TryHideLoadingScreen()
    {
        if (loadingScreen == null)
        {
            return;
        }

        if (sceneTransitionPending || backgroundLoadingCount > 0)
        {
            loadingScreen.gameObject.SetActive(true);
            return;
        }

        loadingScreen.HideLoadingScreen();
    }
}