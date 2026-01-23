using UnityEngine;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class WorldCanvasScaler : MonoBehaviour
{
    public float distanceFromCamera = 10f; // distance devant la caméra
    private Canvas worldCanvas;
    private RectTransform rectTransform;

    void Awake()
    {
        worldCanvas = GetComponent<Canvas>();
        rectTransform = GetComponent<RectTransform>();

        if(worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning("Canvas n’est pas en World Space ! Changement automatique.");
            worldCanvas.renderMode = RenderMode.WorldSpace;
        }
        rectTransform.localScale = Vector3.one; // scale 1 pour correspondre aux unités Unity

        UpdateCanvasSize();
    }

    void UpdateCanvasSize()
    {
        Camera cam = Camera.main;
        if(cam.orthographic) 
        {
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;

            rectTransform.sizeDelta = new Vector2(width, height);
        }
        else
        {
            // pour caméra perspective : calcul approximatif pour remplir l’écran à la distance donnée
            float height = 2f * distanceFromCamera * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * cam.aspect;
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        // placer le canvas devant la caméra
        rectTransform.position = cam.transform.position + cam.transform.forward * distanceFromCamera;
        rectTransform.rotation = cam.transform.rotation; // aligner le canvas sur la caméra
    }

    // Optional : mettre à jour en live si l’appareil change de résolution
    void LateUpdate()
    {
        UpdateCanvasSize();
    }
}
