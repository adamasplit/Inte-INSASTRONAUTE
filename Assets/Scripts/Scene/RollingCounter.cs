using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RollingCounter : MonoBehaviour
{
    public List<RollingDigit> digits = new List<RollingDigit>();
    public GameObject digitPrefab;
    public long currentValue = 0;
    public bool finishedAnimating = false;
    public Image image;

    public void SetInstant(long value)
    {
        foreach(var d in digits)
            Destroy(d.gameObject);
        digits.Clear();
        string s = value.ToString().PadLeft(digits.Count,'0');
        for (int i = 0; i < s.Length; i++)
        {
            GameObject go = Instantiate(digitPrefab, transform);
            digits.Add(go.GetComponent<RollingDigit>());
            go.GetComponent<RollingDigit>().SetDigitInstant(s[i] - '0');
        }
        currentValue = value;
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

    public async Task AnimateFromTo(long startValue, long endValue, bool withImage = true,bool instantEnd = false)
    {
        Debug.Log($"Animating from {startValue} to {endValue}");
        currentValue = endValue;
        if (endValue < 0)
        {
            Debug.LogWarning("End value is negative, setting to 0");
            endValue = 0;
        }
        
        GetComponent<CanvasGroup>().alpha = 1;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        GetComponent<CanvasGroup>().interactable = true;
        if (image != null)
            image.enabled = withImage;
        if (instantEnd)
        {
            EndAnimationInstant(endValue);
            return;
        }
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
        while(!finishedAnimating)        {
            await Task.Delay(100);
        }
        // Ensure all digits are set to their final value and visible
        for(int i = 0; i < digits.Count; i++)
        {
            long targetDigit = target[i] - '0';
            digits[i].SetDigitInstant(targetDigit);
        }
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