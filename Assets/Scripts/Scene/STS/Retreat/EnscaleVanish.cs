using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class EnscaleVanish : MonoBehaviour
{
    public float vanishDuration = 0.5f;
    public float vanishDelay = 0.5f;
    public Image image;
    public void StartVanish()
    {
        StartCoroutine(Vanish());
    }

    IEnumerator Vanish()
    {

        float elapsed = 0f;
        Vector3 initialScale = image.transform.localScale;

        while (elapsed < vanishDuration)
        {
            float t = Mathf.Clamp01(elapsed / vanishDuration);
            image.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one*2, t);
            if (elapsed>vanishDuration/2f)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Lerp(1f, 0f, (elapsed - vanishDuration / 2f) / (vanishDuration / 2f)));
            }
            else
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, Mathf.Lerp(1f, 0f, t));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        image.transform.localScale = Vector3.zero;
    }
}