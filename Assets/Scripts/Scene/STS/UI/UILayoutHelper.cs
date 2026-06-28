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

    public static void ApplyPreferredSize(RectTransform rectTransform, bool fitWidth = true, bool fitHeight = true, float extraWidth = 0f, float extraHeight = 0f)
    {
        if (rectTransform == null)
            return;

        LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();

        float previousPreferredWidth = layoutElement.preferredWidth;
        float previousPreferredHeight = layoutElement.preferredHeight;

        if (fitWidth)
            layoutElement.preferredWidth = -1f;

        if (fitHeight)
            layoutElement.preferredHeight = -1f;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();

        if (fitWidth)
        {
            float preferredWidth = LayoutUtility.GetPreferredWidth(rectTransform);
            if (preferredWidth > 0f)
                layoutElement.preferredWidth = Mathf.Ceil(preferredWidth + extraWidth);
            else
                layoutElement.preferredWidth = previousPreferredWidth;
        }

        if (fitHeight)
        {
            float preferredHeight = LayoutUtility.GetPreferredHeight(rectTransform);
            if (preferredHeight > 0f)
                layoutElement.preferredHeight = Mathf.Ceil(preferredHeight + extraHeight);
            else
                layoutElement.preferredHeight = previousPreferredHeight;
        }

        RectTransform parentRectTransform = rectTransform.parent as RectTransform;
        if (parentRectTransform != null)
            LayoutRebuilder.MarkLayoutForRebuild(parentRectTransform);

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    public static Coroutine ApplyPreferredSizeAfterFrame(
        MonoBehaviour host,
        RectTransform rectTransform,
        bool fitWidth = true,
        bool fitHeight = true,
        float extraWidth = 0f,
        float extraHeight = 0f)
    {
        if (host == null || rectTransform == null)
            return null;

        return host.StartCoroutine(ApplyPreferredSizeAfterFrameCoroutine(rectTransform, fitWidth, fitHeight, extraWidth, extraHeight));
    }

    private static IEnumerator ApplyPreferredSizeAfterFrameCoroutine(
        RectTransform rectTransform,
        bool fitWidth,
        bool fitHeight,
        float extraWidth,
        float extraHeight)
    {
        yield return null;
        ApplyPreferredSize(rectTransform, fitWidth, fitHeight, extraWidth, extraHeight);
    }

    public static Coroutine ApplyActualSizeAfterFrame(
        MonoBehaviour host,
        RectTransform rectTransform,
        float extraWidth = 0f,
        float extraHeight = 0f)
    {
        if (host == null || rectTransform == null)
            return null;

        return host.StartCoroutine(ApplyActualSizeAfterFrameCoroutine(rectTransform, extraWidth, extraHeight));
    }

    public static Coroutine ApplyChildActualSizeAfterFrame(
        MonoBehaviour host,
        RectTransform parentRectTransform,
        float extraWidth = 0f,
        float extraHeight = 0f)
    {
        if (host == null || parentRectTransform == null)
            return null;

        return host.StartCoroutine(ApplyChildActualSizeAfterFrameCoroutine(parentRectTransform, extraWidth, extraHeight));
    }

    private static IEnumerator ApplyChildActualSizeAfterFrameCoroutine(
        RectTransform parentRectTransform,
        float extraWidth,
        float extraHeight)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        RectTransform contentRectTransform = null;
        if (parentRectTransform.childCount > 0)
            contentRectTransform = parentRectTransform.GetChild(0) as RectTransform;

        if (contentRectTransform == null)
            yield break;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
        Canvas.ForceUpdateCanvases();

        LayoutElement layoutElement = parentRectTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = parentRectTransform.gameObject.AddComponent<LayoutElement>();

        float contentWidth = contentRectTransform.rect.width;
        float contentHeight = contentRectTransform.rect.height;

        if (contentWidth > 0f)
            layoutElement.preferredWidth = Mathf.Ceil(contentWidth + extraWidth);

        if (contentHeight > 0f)
            layoutElement.preferredHeight = Mathf.Ceil(contentHeight + extraHeight);

        LayoutRebuilder.MarkLayoutForRebuild(parentRectTransform);
    }

    private static IEnumerator ApplyActualSizeAfterFrameCoroutine(
        RectTransform rectTransform,
        float extraWidth,
        float extraHeight)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();

        LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();

        float actualWidth = rectTransform.rect.width;
        float actualHeight = rectTransform.rect.height;

        if (actualWidth > 0f)
            layoutElement.preferredWidth = Mathf.Ceil(actualWidth + extraWidth);

        if (actualHeight > 0f)
            layoutElement.preferredHeight = Mathf.Ceil(actualHeight + extraHeight);

        RectTransform parentRectTransform = rectTransform.parent as RectTransform;
        if (parentRectTransform != null)
            LayoutRebuilder.MarkLayoutForRebuild(parentRectTransform);

        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}