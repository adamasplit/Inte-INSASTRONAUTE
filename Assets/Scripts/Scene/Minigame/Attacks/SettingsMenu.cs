using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public Toggle vfxToggle;

    const string VFX_KEY = "VFXEnabled";

    void Start()
    {
        vfxToggle.isOn = GameSettings.VFXEnabled;
    }

    public void OnToggleChanged(bool value)
    {
        GameSettings.VFXEnabled = value;
    }
}