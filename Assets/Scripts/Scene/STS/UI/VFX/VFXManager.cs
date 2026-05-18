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
            Vector3 RandomVector = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            GameObject vfxInstance = Instantiate(prefab, position+RandomVector, Quaternion.identity, vfxCanvas.transform);
            StartCoroutine(DestroyAfterDuration(vfxInstance, 2f)); // Adjust duration as needed
        }
    }
    private System.Collections.IEnumerator DestroyAfterDuration(GameObject obj, float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(obj);
    }
}