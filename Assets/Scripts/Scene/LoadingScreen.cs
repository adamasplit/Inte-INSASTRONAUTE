using UnityEngine;
using TMPro;
public class LoadingScreen : MonoBehaviour
{
    public TextMeshProUGUI loadingText;

    private void Start()
    {
        loadingText.text = "Chargement...";
    }
    private void Update()
    {
        // Optionnel : ajouter des points de suspension animés
        float t = Time.time % 1f;
        int dots = (int)(t * 4);
        loadingText.text = "Chargement" + new string('.', dots);
    }
}