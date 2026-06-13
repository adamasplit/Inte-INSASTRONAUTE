using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class STSSceneLoader : MonoBehaviour
{
    public static STSSceneLoader Instance { get; private set; }
    public STSLoadingScreen loadingScreen;

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

    public void LoadScene(string sceneName)
    {
        // Start async loading and update the loading screen progress
        StartCoroutine(LoadSceneAsyncRoutine(sceneName));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
            loadingScreen.SetProgress(0f);
        }

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
            // Give one frame to render the completed progress
            yield return null;
            loadingScreen.HideLoadingScreen();
        }
    }

    public void EndLoading()
    {
        if (loadingScreen != null)
        {
            loadingScreen.HideLoadingScreen();
        }
    }
}