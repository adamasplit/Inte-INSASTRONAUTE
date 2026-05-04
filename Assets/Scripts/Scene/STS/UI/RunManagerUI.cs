using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class RunManagerUI : MonoBehaviour
{
    public TextMeshProUGUI floorText;
    public TextMeshProUGUI actText;
    public TextMeshProUGUI hpText;
    void Update()
    {
        if (RunManager.Instance == null) return;

        floorText.text = $"Étage {RunManager.Instance.currentFloor}";
        actText.text = $"Acte {RunManager.Instance.act + 1}";
        hpText.text = $"PV : {RunManager.Instance.player.currentHP}/{RunManager.Instance.player.maxHP}";
    }
}