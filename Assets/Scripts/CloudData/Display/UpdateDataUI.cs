using UnityEngine;
using System.Threading.Tasks;
using TMPro;

public class UpdateDataUI : MonoBehaviour
{
    public string dataKey;
    public void RefreshDataUI()
    {
        switch (dataKey)
        {
            case "TOKEN":
                GetComponent<TextMeshProUGUI>().text = PlayerProfileStore.TOKEN.ToString();
                break;
            case "PC":
                GetComponent<TextMeshProUGUI>().text = PlayerProfileStore.PC.ToString();
                break;
            case "USERNAME":
                GetComponent<TextMeshProUGUI>().text = PlayerProfileStore.DISPLAY_NAME;
                break;
            default:
                Debug.LogWarning("Unknown data key: " + dataKey);
                break;
        }
    }
}