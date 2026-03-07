using UnityEngine;
public class AutoDestroyVFX : MonoBehaviour
{
    public float duration = 1f;
    void Awake()
    {
        Destroy(gameObject, duration);
    }
}