
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class STSTutorialMenu : MonoBehaviour
{
    public TMP_Dropdown tutorialStageDropdown;
    public void StartTutorial()
    {
        RunManager.Instance.StartTutorialRun();
    }
}