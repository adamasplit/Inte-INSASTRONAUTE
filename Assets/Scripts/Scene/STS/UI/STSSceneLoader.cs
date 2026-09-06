using System;
using System.Collections;
using System.Threading.Tasks;
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
    private int sceneTransitionToken = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void BeginLoading(string statusText = null, bool canCancel = false, Action onCancel = null)
    {
        if (backgroundLoadingCount == 0)
        {
            backgroundProgress = 0f;
        }

        backgroundLoadingCount++;
        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
            loadingScreen.SetLoadingState(statusText, canCancel, onCancel);
            ApplyProgressToScreen();
        }
    }

    public void SetLoadingState(string statusText, bool canCancel = false, Action onCancel = null)
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetLoadingState(statusText, canCancel, onCancel);
        }
    }

    public void SetBackgroundProgress(float progress)
    {
        backgroundProgress = Mathf.Max(backgroundProgress, Mathf.Clamp01(progress));
        ApplyProgressToScreen();
    }

    /// <summary>
    /// Charge la base de cartes en faisant avancer la barre entre <paramref name="from"/> et
    /// <paramref name="to"/> au fil des illustrations téléchargées, au lieu de rester figée sur
    /// <paramref name="from"/> pendant tout le chargement. Quand la base est déjà en mémoire,
    /// aucun événement n'est émis et la barre saute directement à <paramref name="to"/>.
    /// </summary>
    public static async Task LoadCardDatabaseWithProgressAsync(float from, float to)
    {
        // L'attente du catalogue est indivisible : on la compte comme la première part de
        // l'intervalle, puis chaque illustration reçue fait avancer le reste.
        const float catalogShare = 0.15f;

        void OnSpriteProgress(float progress)
        {
            Instance?.SetBackgroundProgress(Mathf.Lerp(from, to, catalogShare + (1f - catalogShare) * progress));
        }

        STSCardDatabase.CollectionCardSpriteProgress += OnSpriteProgress;
        try
        {
            await STSCardDatabase.LoadAsync();
        }
        finally
        {
            STSCardDatabase.CollectionCardSpriteProgress -= OnSpriteProgress;
        }

        Instance?.SetBackgroundProgress(to);
    }

    public void LoadScene(string sceneName)
    {
        sceneTransitionPending = true;
        sceneTransitionProgress = 0f;
        sceneTransitionToken++;
        sceneStartProgress = backgroundLoadingCount > 0
            ? Mathf.Clamp01(backgroundProgress)
            : 0f;

        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(true);
            ApplyProgressToScreen();
        }
        StartCoroutine(LoadSceneAsyncRoutine(sceneName, sceneTransitionToken));
    }

    private IEnumerator LoadSceneAsyncRoutine(string sceneName, int transitionToken)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        while (!op.isDone)
        {
            float progress = op.progress;
            sceneTransitionProgress = Mathf.Clamp01(progress / 0.9f);
            ApplyProgressToScreen();
            yield return null;
        }

        sceneTransitionProgress = 1f;
        ApplyProgressToScreen();

        yield return null;
        if (transitionToken != sceneTransitionToken)
        {
            yield break;
        }

        if (sceneTransitionPending && backgroundLoadingCount == 0)
        {
            SceneReady();
        }
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