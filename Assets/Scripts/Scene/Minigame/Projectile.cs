using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    public float speed = 800f;

    private Enemy target;
    private Action<Enemy> onHit;

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

        Vector3 targetPos = target.transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            onHit?.Invoke(target);
            Destroy(gameObject);
        }
    }
}
