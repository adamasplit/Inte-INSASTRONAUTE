using UnityEngine;
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }
    public Canvas vfxCanvas;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void PlayEffect(EffectEntry entry, Vector3 position)
    {
        GameObject prefab= entry.GetVFXPrefab();
        if (prefab != null)        {
            Instantiate(prefab, position, Quaternion.identity, vfxCanvas.transform);
        }
    }
}