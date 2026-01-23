using UnityEngine;
using System.Threading.Tasks;
using TMPro;

public class UpdateDataUI : MonoBehaviour
{
    public string dataKey;
    public long currentValue;
    public void RefreshDataUI()
    {
        switch (dataKey)
        {
            case "TOKEN":
                GetComponent<TextMeshProUGUI>().text = PlayerProfileStore.TOKEN.ToString();
                currentValue = PlayerProfileStore.TOKEN;
                break;
            case "PC":
                GetComponent<TextMeshProUGUI>().text = PlayerProfileStore.PC.ToString();
                currentValue = PlayerProfileStore.PC;
                break;
            case "USERNAME":
                GetComponent<TextMeshProUGUI>().text = PlayerProfileStore.DISPLAY_NAME;
                break;
            default:
                Debug.LogWarning("Unknown data key: " + dataKey);
                break;
        }
    }
    public void alterDataUI(long value)
    {
        setDataUI(currentValue + value);
    }

    public void setDataUI(long value)
    {
        switch (dataKey)
        {
            case "TOKEN":
                GetComponent<TextMeshProUGUI>().text = value.ToString();
                currentValue = value;
                break;
            case "PC":
                GetComponent<TextMeshProUGUI>().text = value.ToString();
                currentValue = value;
                break;
            default:
                Debug.LogWarning("Unknown data key for forceDataUI: " + dataKey);
                break;
        }
    }

}