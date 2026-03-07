using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    public float speed = 800f;
    public float waveAmplitude = 0f;
    public float lingerTime = 0f;

    private Enemy target;
    private Action<Enemy> onHit;
    private bool hasHit = false;

    public void Init(Enemy target, Action<Enemy> onHit)
    {
        this.target = target;
        this.onHit = onHit;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        if (hasHit)
        {
            transform.position += Vector3.up * speed/10f * Time.deltaTime+Vector3.right* Mathf.Sin(Time.time * 20f) * waveAmplitude; // Move upwards while lingering
            return;
        }
        Vector3 targetPos = target.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        transform.position += Vector3.right * Mathf.Sin(Time.time * 20f) * waveAmplitude; // effet de vague
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            hasHit = true;
            onHit?.Invoke(target);
            //StartCoroutine(HitTarget());
        }
    }

    private System.Collections.IEnumerator HitTarget()
    {
        onHit?.Invoke(target);
        // Detach TrailRenderer so it can fade out naturally
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.transform.parent = null;
            // Optionally, destroy the trail after it has faded
            Destroy(trail.gameObject, trail.time);
        }
        yield return null;
    }
}
