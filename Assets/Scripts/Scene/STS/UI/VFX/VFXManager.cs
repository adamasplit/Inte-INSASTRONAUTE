using UnityEngine;
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }
    public Canvas vfxCanvas;
    public UIManager uiManager;
    public RectTransform energyRoot;
    [Range(0f, 180f)] public float damageSlashAngleRandomRange = 25f;
    public int activeEffectsCount = 0;
    public bool activeEffects => activeEffectsCount > 0;
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
            Quaternion rotation = GetSpawnRotation(entry != null ? entry.GetEffectName() : null);
            GameObject vfxInstance = Instantiate(prefab, position+RandomVector, rotation, vfxCanvas.transform);
            StartCoroutine(DestroyAfterDuration(vfxInstance, 2f)); // Adjust duration as needed
        }
    }
    public void PlayEffect(string effectName, Vector3 position)
    {
        GameObject prefab = Resources.Load<GameObject>($"STS/VFX/{effectName}");
        if (prefab != null)
        {
            Vector3 RandomVector = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            Quaternion rotation = GetSpawnRotation(effectName);
            GameObject vfxInstance = Instantiate(prefab, position+RandomVector, rotation, vfxCanvas.transform);
            StartCoroutine(DestroyAfterDuration(vfxInstance, 2f)); // Adjust duration as needed
        }
    }
    public void PlayEffect(string effectName, Character character)
    {
        if (character == null) return;
        if (uiManager.GetView(character) == null) return;
        Vector3 position =uiManager.GetView(character).position;
        PlayEffect(effectName, position);
    }
    public void AnimateEnergyGain()
    {
        GameObject prefab = Resources.Load<GameObject>($"STS/VFX/Energy");
        if (prefab != null)
        {
            Vector3 position = energyRoot.position;
            GameObject vfxInstance = Instantiate(prefab, position, Quaternion.identity, vfxCanvas.transform);
            StartCoroutine(DestroyAfterDuration(vfxInstance, 2f)); // Adjust duration as needed
        }
    }

    private Quaternion GetSpawnRotation(string effectName)
    {
        if (string.Equals(effectName, "DamageSlash", System.StringComparison.OrdinalIgnoreCase))
        {
            float angle = Random.Range(-damageSlashAngleRandomRange, damageSlashAngleRandomRange);
            return Quaternion.Euler(0f, 0f, angle);
        }

        return Quaternion.identity;
    }

    private System.Collections.IEnumerator DestroyAfterDuration(GameObject obj, float duration)
    {
        activeEffectsCount++;
        yield return new WaitForSeconds(duration);
        Destroy(obj);
        activeEffectsCount--;
    }
}