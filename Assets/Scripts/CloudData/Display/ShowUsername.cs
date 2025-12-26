using UnityEngine;
using TMPro;

public class ShowUsername : MonoBehaviour
{
    private void Start()
    {
        string username = PlayerProfileStore.DISPLAY_NAME;
        TMP_Text textComponent = GetComponent<TMP_Text>();
        if (!string.IsNullOrEmpty(username))
        {
            if (textComponent != null)
            {
                textComponent.text = username;
            }
            else
            {
                Debug.LogWarning("TMP_Text component not found on this GameObject.");
            }
        }
        else
        {
            Debug.LogWarning("Username is null or empty.");
            if (textComponent != null)
                textComponent.text = "Guest";
        }
    }
}