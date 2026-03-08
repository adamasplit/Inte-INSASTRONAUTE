using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RollingCounter : MonoBehaviour
{
    public List<RollingDigit> digits = new List<RollingDigit>();
    public GameObject digitPrefab;
    public bool finishedAnimating = false;

    public void SetInstant(long value)
    {
        string s = value.ToString().PadLeft(digits.Count,'0');

        for(int i = 0; i < digits.Count; i++)
        {
            long d = s[i] - '0';
            digits[i].SetDigitInstant(d);
        }
    }

    public void EndAnimationInstant(long value)
    {       
        SetInstant(value);
        finishedAnimating = true;
    }

    public void endAnimationInstant()
    {       
        finishedAnimating = true;
        Debug.Log("Animation ended instantly");
    }

    public async Task AnimateFromTo(long startValue, long endValue)
    {
        GetComponent<CanvasGroup>().alpha = 1;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        GetComponent<CanvasGroup>().interactable = true;
        finishedAnimating = false;
        foreach(var d in digits)
            Destroy(d.gameObject);
        for(int i = 0; i < endValue.ToString().Length; i++)
        {
            GameObject go = Instantiate(digitPrefab, transform);
            digits.Add(go.GetComponent<RollingDigit>());
            digits[i].SetDigitInstant(0);
        }
        //SetInstant(startValue);
        await Task.Delay(500); // Small delay before starting the animation

        string target = endValue.ToString().PadLeft(digits.Count,'0');

        for(int i = digits.Count-1; i >= 0; i--)
        {
            if (finishedAnimating)
                break;
            long targetDigit = target[i] - '0';
            StartCoroutine(digits[i].RollTo(targetDigit, (i+1)*0.35f));
            // No else: don't set instant here, will do after animation
            await Task.Delay(100);
        }
        if (!finishedAnimating)
            await Task.Delay(1500);
        // Ensure all digits are set to their final value and visible
        for(int i = 0; i < digits.Count; i++)
        {
            long targetDigit = target[i] - '0';
            digits[i].SetDigitInstant(targetDigit);
        }
        finishedAnimating = true;
        GetComponent<CanvasGroup>().alpha = 0;
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        GetComponent<CanvasGroup>().interactable = false;
    }

    public void Clear()
    {
        foreach(var d in digits)
            Destroy(d.gameObject);
        digits.Clear();
    }
}