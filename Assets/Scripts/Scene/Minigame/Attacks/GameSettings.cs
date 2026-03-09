using UnityEngine;
using UnityEngine.UI;
public static class GameSettings
{
    public static bool VFXEnabled
    {
        get => PlayerPrefs.GetInt("VFXEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("VFXEnabled", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}