using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VineAttackVFX : MonoBehaviour, IVFX
{
    public LineRenderer vine;
    public Vector3 startPoint;
    public Enemy target;

    public int segments = 20;
    public float growDuration = 0.4f;

    public float waveAmplitude = 0.5f;
    public float waveFrequency = 4f;

    public float straightenSpeed = 2f;
    private float firstControlPointOffset;
    private float secondControlPointOffset;

    private float currentAmplitude;
    private CardData cardData;

    void Start()
    {
        vine.positionCount = segments;
        currentAmplitude = waveAmplitude;
    }

    public void Fire(Vector3 startPos, List<Enemy> targetPos, CardData card)
    {
        startPoint = startPos;
        cardData = card;
        target = targetPos[0]; // Assuming the first target is the primary one
        firstControlPointOffset = Random.Range(-0.4f, 0.4f);
        secondControlPointOffset = Random.Range(-0.4f, 0.4f);
        StartCoroutine(GrowVine());
    }

    IEnumerator GrowVine()
    {
        target.Halt(); 
        float t = 0;

        while (t < growDuration)
        {
            t += Time.deltaTime;

            float progress = t / growDuration;

            UpdateVine(progress);

            yield return null;
        }

        // Une fois arrivée, on raidit
        while (currentAmplitude > 0.01f)
        {
            currentAmplitude = Mathf.Lerp(currentAmplitude, 0, Time.deltaTime * straightenSpeed);
            UpdateVine(1f);
            yield return null;
        }

        currentAmplitude = 0;
        UpdateVine(1f);
        target.TakeDamage(cardData.baseDamage, cardData.element);
        target.Resume();
        t=0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            vine.widthMultiplier = Mathf.Lerp(1f, 0f, t / 0.2f);
            yield return null;
        }
        Destroy(gameObject);
    }

    void UpdateVine(float progress)
    {
        vine.positionCount = segments;

        Vector3 start = startPoint;
        Vector3 end = Vector3.Lerp(startPoint, target.transform.position, progress);

        Vector3 control1 = GetControlPoint(start, end, firstControlPointOffset);
        Vector3 control2 = GetControlPoint(start, end, secondControlPointOffset);

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            Vector3 pos = BezierCubic(start, control1, control2, end, t);

            // direction locale pour l'ondulation
            Vector3 dir = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(dir, Vector3.forward);

            float wave = Mathf.Sin(t * waveFrequency * Mathf.PI * 2) * currentAmplitude;

            pos += perpendicular * wave;

            vine.SetPosition(i, pos);
        }
    }

    Vector3 BezierCubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1 - t;
        return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
    }

    Vector3 GetControlPoint(Vector3 start, Vector3 end, float curveFactor)
    {
        Vector3 mid = (start + end) * 0.5f;
        Vector3 dir = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(dir, Vector3.forward);
        float curveAmount = Vector3.Distance(start, end) * curveFactor;
        return mid + perpendicular * curveAmount;
    }
}