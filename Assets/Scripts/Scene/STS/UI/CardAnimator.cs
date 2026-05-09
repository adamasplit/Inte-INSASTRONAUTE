using UnityEngine;
using System.Collections;

public class CardAnimator : MonoBehaviour
{
    public float duration = 0.15f;
    public float curveHeight = 40f;
    public RectTransform animationLayer;

    public IEnumerator MoveCard(
        RectTransform rect,
        Vector3 start,
        Vector3 end,
        float speedMultiplier = 1f,
        bool curved = true,
        bool forceRotation = false
    )
    {
        float t = 0f;
        Quaternion startRotation = rect.localRotation;


        Vector3 control =
            (start + end) / 2
            + Vector3.up * curveHeight;

        if (!curved)
            control = (start + end) / 2;
        while (t < 1f)
        {
            t += Time.deltaTime / duration * speedMultiplier;

            float eased =
                Mathf.SmoothStep(0, 1, t);

            Vector3 pos =
                Mathf.Pow(1 - eased, 2) * start
                + 2 * (1 - eased) * eased * control
                + Mathf.Pow(eased, 2) * end;

            rect.position = pos;
            if (forceRotation)
            {
                rect.localRotation = Quaternion.Lerp(
                    startRotation,
                    Quaternion.identity,
                    eased
                );
            }

            yield return null;
        }

        rect.position = end;
        if (forceRotation)
        {
            rect.localRotation = Quaternion.identity;
        }
    }
}