using UnityEngine;

public class ResponsiveStarfield : MonoBehaviour
{
    public ParticleSystem starfield;
    private ParticleSystem.ShapeModule shapeModule;

    void Start()
    {
        shapeModule = starfield.shape;
        AdjustParticleBoxSize();
    }

    void Update()
    {
        // Si la résolution change en cours de jeu, on réajuste
        AdjustParticleBoxSize();
    }

    void AdjustParticleBoxSize()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Ajuste la taille de la Shape Box en fonction de la résolution
        shapeModule.scale = new Vector3(screenWidth / 100f, screenHeight / 100f, 1); // Ajuste les valeurs selon tes besoins
    }
}
