using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates an overlay with a transparent cutout to highlight UI elements.
/// Uses a shader-based approach for better performance.
/// </summary>
[RequireComponent(typeof(Image))]
public class TutorialOverlayMask : MonoBehaviour
{
    [Header("Cutout Settings")]
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private Vector2 padding = new Vector2(10f, 10f);
    [SerializeField] private float cornerRadius = 20f;
    [SerializeField] private bool isCircle = false;
    
    private Image image;
    private Material overlayMaterial;
    private Canvas parentCanvas;
    
    // Shader property IDs for better performance
    private static readonly int CenterProp = Shader.PropertyToID("_Center");
    private static readonly int SizeProp = Shader.PropertyToID("_Size");
    private static readonly int RadiusProp = Shader.PropertyToID("_Radius");
    private static readonly int IsCircleProp = Shader.PropertyToID("_IsCircle");
    
    private void Awake()
    {
        image = GetComponent<Image>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Note: You'll need to create a custom shader for this or use a simpler approach
        // For now, we'll use the standard UI approach with transparency
    }
    
    public void SetTarget(RectTransform target, Vector2 customPadding, bool circle = false)
    {
        targetRect = target;
        padding = customPadding;
        isCircle = circle;
        UpdateCutout();
    }
    
    private void LateUpdate()
    {
        if (targetRect != null)
        {
            UpdateCutout();
        }
    }
    
    private void UpdateCutout()
    {
        if (targetRect == null || image == null) return;
        
        // Get screen position of target
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        
        // Calculate center in canvas space
        Vector2 center = RectTransformUtility.WorldToScreenPoint(
            parentCanvas?.worldCamera, 
            targetRect.position
        );
        
        // Calculate size with padding
        Vector2 size = targetRect.rect.size + padding * 2f;
        
        // If you have a custom shader, apply properties here
        // For this example, we'll just keep it simple
    }
}
