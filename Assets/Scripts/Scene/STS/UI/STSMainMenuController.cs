using UnityEngine;
using UnityEngine.UI;

public class STSMainMenuController : MonoBehaviour
{
    public Button loadButton;
    public string resumeSceneName = "STS_Map";

    void Start()
    {
        RefreshLoadButtonState();
    }

    void OnEnable()
    {
        RefreshLoadButtonState();
    }

    public void RefreshLoadButtonState()
    {
        if (loadButton != null)
        {
            loadButton.gameObject.SetActive(STSRunSaveSystem.HasLoadableSave());
        }
    }

    public async void LoadSavedRun()
    {
        if (!STSRunSaveSystem.HasLoadableSave())
            return;

        if (RunManager.Instance == null)
        {
            new GameObject("RunManager").AddComponent<RunManager>();
        }

        await STSCardDatabase.LoadAsync();

        if (!RunManager.Instance.LoadSavedRun())
            return;

        STSSceneLoader.Instance?.LoadScene(resumeSceneName);
    }
}