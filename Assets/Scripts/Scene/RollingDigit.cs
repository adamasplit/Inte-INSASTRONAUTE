using UnityEngine;
using System.Collections;

public class RollingDigit : MonoBehaviour
{
    public RectTransform numbers;
    public float digitHeight = 60f;
    public float duration = 0.15f;

    long currentDigit = 0;

    public void SetDigitInstant(long value)
    {
        currentDigit = value;
        numbers.anchoredPosition = new Vector2(0, value * digitHeight);
    }

    public IEnumerator RollTo(long target,float speedMultiplier = 1f)
    {
        while (currentDigit != target)
        {
            long next = (currentDigit + 1) % 10;

            float startY = currentDigit * digitHeight;
            float endY = next * digitHeight;

            float t = 0;

            while (t < duration)
            {
                t += Time.deltaTime * speedMultiplier;
                float p = Mathf.SmoothStep(0,1,t/duration);

                float y = Mathf.Lerp(startY,endY,p);
                numbers.anchoredPosition = new Vector2(0,y);

                yield return null;
            }

            currentDigit = next;
        }
    }

    public long GetDigit()
    {
        return currentDigit;
    }
}