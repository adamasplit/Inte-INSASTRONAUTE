using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public void LoadMain()
    {
        StartCoroutine(LoadMainCoroutine());
    }

    private IEnumerator LoadMainCoroutine()
    {
        Debug.Log("Loading Main async...");
        var op = SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);

        // optionnel : empêcher l’activation immédiate si tu veux afficher un %,
        // sinon tu peux laisser true direct
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            // op.progress va jusqu’à ~0.9 avant activation dans certains cas
            Debug.Log($"Loading progress: {op.progress}");
            yield return null;
        }

        Debug.Log("Main loaded.");
    }
}
