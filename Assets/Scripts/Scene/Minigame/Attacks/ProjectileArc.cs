using UnityEngine;
using System;

public class ProjectileArc : MonoBehaviour
{
    public float duration = 0.6f;
    public AnimationCurve timeCurve;
    public float arcHeight = 200f;
    public float backwardOffset = 80f;

    private Vector3 start;
    private Vector3 control;
    private Vector3 end;

    private float timer;
    private Action onHit;

    public void Init(Enemy target, Action<Enemy> onHit)
    {
        this.onHit = () => onHit?.Invoke(target);

        start = transform.position;
        if (target == null)
        {
            Debug.LogError("Target is null in ProjectileArc.Init");
            Destroy(gameObject);
            return;
        }
        end = target.transform.position;

        Vector3 dir = (end - start).normalized;

        // ⬆️ "haut" de l’arc = perpendiculaire à la direction de tir
        Vector3 arcUp = Vector3.Cross(dir, Vector3.forward).normalized;

        // si jamais dir ~ forward (sécurité)
        if (arcUp.sqrMagnitude < 0.001f)
            arcUp = Vector3.up;
        if (UnityEngine.Random.value < 0.5f)
        arcUp = -arcUp;

        control =
            start
            - dir * backwardOffset   // léger recul
            + arcUp * arcHeight;     // arc propre
        
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float u = timeCurve != null ? timeCurve.Evaluate(t) : t;

        // Bezier quadratique stable
        Vector3 pos =
            (1 - u) * (1 - u) * start +
            2 * (1 - u) * u * control +
            u * u * end;

        transform.position = pos;

        // orientation vers la trajectoire
        Vector3 tangent =
            2 * (1 - u) * (control - start) +
            2 * u * (end - control);

        if (tangent.sqrMagnitude > 0.001f)
            transform.up = tangent.normalized;

        if (t >= 1f)
        {
            onHit?.Invoke();
            Destroy(gameObject);
        }
    }
}
