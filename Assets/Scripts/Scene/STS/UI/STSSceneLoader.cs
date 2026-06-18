using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class STSSceneLoader : MonoBehaviour
{
    public static STSSceneLoader Instance { get; private set; }
    public STSLoadingScreen loadingScreen;
    private int backgroundLoadingCount = 0;
    private bool sceneTransitionPending = false;

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
        backgroundLoadingCount++;
        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
        }
    }

    public void LoadScene(string sceneName)
    {
        // Start async loading and update the loading screen progress.
        sceneTransitionPending = true;
        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
            loadingScreen.SetProgress(0f);
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
            float normalized = Mathf.Clamp01(progress / 0.9f);
            if (loadingScreen != null)
            {
                loadingScreen.SetProgress(normalized);
            }
            yield return null;
        }

        // Ensure progress reaches 100%
        if (loadingScreen != null)
        {
            loadingScreen.SetProgress(1f);
        }
    }

    public void EndLoading()
    {
        if (backgroundLoadingCount > 0)
        {
            backgroundLoadingCount--;
        }

        TryHideLoadingScreen();
    }

    public void SceneReady()
    {
        sceneTransitionPending = false;
        TryHideLoadingScreen();
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