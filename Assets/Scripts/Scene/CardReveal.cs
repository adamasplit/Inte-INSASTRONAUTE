using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
public class CardReveal:MonoBehaviour
{
    public Transform cardRoot;
    public Image cardImage;
    public Image effectImage;
    public void RevealCard()
    {
        effectImage.sprite = cardImage.sprite;
        StartCoroutine(RevealCardRoutine());
    }

    public IEnumerator RevealCardRoutine()
    {
        // Simple reveal animation
        float duration = 0.1f;
        float elapsed = 0f;
        Vector3 initialScale = new Vector3(0f, 1f, 1f);
        Vector3 targetScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cardRoot.localScale = Vector3.Lerp(initialScale, targetScale, t);
            yield return null;
        }
        cardRoot.localScale = targetScale;
        elapsed = 0f;
        duration=1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            effectImage.color = new Color(1f, 1f, 1f,Mathf.Lerp(1f, 0f, t));
            effectImage.transform.localScale = (1.5f-0.5f*Mathf.Lerp(1f, 0f, t))*Vector3.one;
            yield return null;
        }
    }
}