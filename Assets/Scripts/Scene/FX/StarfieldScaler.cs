using UnityEngine;
using UnityEngine.UI;

public class StarfieldScaler : MonoBehaviour
{
    public Slider sizeSlider;  // Slider pour ajuster la taille
    public ParticleSystem starfield;  // Référence au système de particules
    private ParticleSystem.ShapeModule shapeModule;

    void Start()
    {
        // Récupère la référence au module Shape du système de particules
        shapeModule = starfield.shape;

        // Initialise le slider avec une valeur par défaut
        sizeSlider.onValueChanged.AddListener(OnSliderValueChanged);
        OnSliderValueChanged(sizeSlider.value);  // Ajuste immédiatement la taille selon le slider
    }

    void OnSliderValueChanged(float value)
    {
        // Ajuste la taille de la box (dépend de la valeur du Slider)
        float newSize = Mathf.Lerp(10f, 200f, value);  // Change les limites selon tes besoins
        shapeModule.scale = new Vector3(newSize, newSize, newSize);
    }
}
