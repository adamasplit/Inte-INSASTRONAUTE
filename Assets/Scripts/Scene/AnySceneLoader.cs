using UnityEngine;
using UnityEngine.SceneManagement;
public class AnySceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}