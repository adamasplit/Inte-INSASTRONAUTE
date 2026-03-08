using System.Collections;
using TMPro;
using UnityEngine;
public class PCText : MonoBehaviour
{
    public TMP_Text text;

    public void SetText(string str)
    {
        text.text = str;
        StartCoroutine(FloatText(text));
    }

    public IEnumerator FloatText(TMP_Text text)
    {
        float t = 0;
        Vector3 start = text.transform.position;

        while(t < 1)
        {
            t += Time.deltaTime;

            text.transform.position = start + Vector3.up * 80 * t;
            text.alpha = 1 - t;

            yield return null;
        }

        Destroy(text.gameObject);
    }
}

