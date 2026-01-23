using UnityEngine;

public class DragLayerProvider : MonoBehaviour
{
    public static Transform Instance;

    void Awake()
    {
        Instance = transform;
    }
}
