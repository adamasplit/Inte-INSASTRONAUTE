using UnityEngine;
public class AutoDestroyVFX : MonoBehaviour
{
    private float duration;

    public void SetDuration(float duration)
    {
        this.duration = duration;
        Invoke(nameof(DestroyVFX), duration);
    }

    private void DestroyVFX()
    {
        Destroy(gameObject);
    }
}