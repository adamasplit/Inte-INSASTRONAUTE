using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class UILayoutHelper
{
    public static Coroutine RebuildAfterFrame(MonoBehaviour host, RectTransform rectTransform)
    {
        if (host == null || rectTransform == null)
            return null;

        return host.StartCoroutine(RebuildAfterFrameCoroutine(rectTransform));
    }

    private static IEnumerator RebuildAfterFrameCoroutine(RectTransform rectTransform)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    public static Coroutine FreezeLayoutAfterFrame(MonoBehaviour host, RectTransform rectTransform)
    {
        if (host == null || rectTransform == null)
            return null;

        return host.StartCoroutine(FreezeLayoutAfterFrameCoroutine(rectTransform));
    }

    private static IEnumerator FreezeLayoutAfterFrameCoroutine(RectTransform rectTransform)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();

        DisableLayoutLocal(rectTransform);
    }

    public static void DisableLayout(RectTransform rectTransform)
    {
        DisableLayoutHierarchy(rectTransform);
    }

    public static void DisableLayoutHierarchy(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        foreach (LayoutGroup layoutGroup in rectTransform.GetComponentsInParent<LayoutGroup>(true))
        {
            if (layoutGroup != null)
                layoutGroup.enabled = false;
        }

        foreach (ContentSizeFitter contentSizeFitter in rectTransform.GetComponentsInParent<ContentSizeFitter>(true))
        {
            if (contentSizeFitter != null)
                contentSizeFitter.enabled = false;
        }
    }

    public static void DisableLayoutLocal(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        foreach (LayoutGroup layoutGroup in rectTransform.GetComponents<LayoutGroup>())
        {
            if (layoutGroup != null)
                layoutGroup.enabled = false;
        }

        foreach (ContentSizeFitter contentSizeFitter in rectTransform.GetComponents<ContentSizeFitter>())
        {
            if (contentSizeFitter != null)
                contentSizeFitter.enabled = false;
        }

        foreach (Transform child in rectTransform)
        {
            LayoutGroup childLayoutGroup = child.GetComponent<LayoutGroup>();
            if (childLayoutGroup != null)
                childLayoutGroup.enabled = false;

            ContentSizeFitter childContentSizeFitter = child.GetComponent<ContentSizeFitter>();
            if (childContentSizeFitter != null)
                childContentSizeFitter.enabled = false;
        }
    }
}