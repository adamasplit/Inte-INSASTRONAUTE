using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public abstract class RewardEntryView : MonoBehaviour
{
    protected RewardItem item;
    protected RewardManager manager;

    public virtual void Init(RewardItem rewardItem, RewardManager mgr)
    {
        item = rewardItem;
        manager = mgr;
    }

    protected IEnumerator Collapse()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        LayoutElement le = GetComponent<LayoutElement>();

        float t = 0;
        float duration = 0.25f;

        float startHeight = le.preferredHeight;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            cg.alpha = 1 - k;
            le.preferredHeight = Mathf.Lerp(startHeight, 0, k);

            yield return null;
        }

        manager.NotifyClaimed(this);

        Destroy(gameObject);
    }
}