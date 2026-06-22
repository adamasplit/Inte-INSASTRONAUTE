using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
public class StatusUI : MonoBehaviour
{
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI valueText;
    public Image icon;
    public Image maskImage;
    public Image buffDebuffIndicator;
    public Image buffDebuffOverlay;
    public Image frame;
    private string statusName;
    private string description;
    private bool tooltipVisible = false;
    private UIManager uiManager;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine spawnAnimation;
    private Coroutine removeAnimation;
    private bool isRemoving;

    public StatusEffect BoundStatus { get; private set; }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void ToggleTooltip()
    {
        tooltipVisible = !tooltipVisible;
        if (tooltipVisible)
        {
            TooltipManager.Instance.ShowTooltip(statusName, description, transform.position);
        }
        else
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
    public void SetStatus(StatusEffect status, UIManager uiManager,bool isPlayer, bool playSpawnAnimation = true)
    {
        this.uiManager = uiManager;
        BoundStatus = status;
        isRemoving = false;
        if (removeAnimation != null)
        {
            StopCoroutine(removeAnimation);
            removeAnimation = null;
        }
        if (spawnAnimation != null)
        {
            StopCoroutine(spawnAnimation);
            spawnAnimation = null;
        }
        canvasGroup.alpha = 1f;
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }
        icon.enabled = true;
        icon.sprite = Resources.Load<Sprite>($"STS/Icons/Status/{status.IconPath()}");
        if (icon.sprite == null)
        {
            icon.sprite = Resources.Load<Sprite>($"STS/Icons/Cards/{status.IconPath()}");
        }
        if (icon.sprite == null)
        {
            icon.enabled = false;
        }
        statusName = status.Name;
        durationText.text = status.Duration > 0 ? status.Duration.ToString() : "";
        valueText.text = status.Value != 0 ? status.Value.ToString() : "";
        valueText.color = Color.white;
        if (status.Value==status.maxValue) // Golden value
        {
            valueText.color = new Color(1f, 0.84f, 0f); // Gold color
        }
        Sprite buffDebuffSprite=null;
        if (status.buff)
        {
            buffDebuffSprite = Resources.Load<Sprite>("STS/Icons/Status3");
            SetFrame(status.framed,status.goldFrame,3);
        }
        else if (status.debuff)
        {
            buffDebuffSprite = Resources.Load<Sprite>("STS/Icons/Status1");
            SetFrame(status.framed,status.goldFrame,1);
        }        
        else
        {
            buffDebuffSprite = Resources.Load<Sprite>("STS/Icons/Status2");
            SetFrame(status.framed,status.goldFrame,2);
        }
        buffDebuffIndicator.sprite = buffDebuffSprite;
        buffDebuffOverlay.sprite = buffDebuffSprite;
        maskImage.sprite = buffDebuffSprite;
        description = status.Desc(isPlayer);

        if (playSpawnAnimation)
        {
            PlaySpawnAnimation();
        }
    }

    public void PlaySpawnAnimation()
    {
        if (spawnAnimation != null)
        {
            StopCoroutine(spawnAnimation);
        }
        spawnAnimation = StartCoroutine(SpawnRoutine());
    }

    public void PlayRemoveAnimationAndDestroy()
    {
        if (isRemoving)
        {
            return;
        }

        isRemoving = true;
        if (removeAnimation != null)
        {
            StopCoroutine(removeAnimation);
        }
        removeAnimation = StartCoroutine(RemoveRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (canvasGroup == null || rectTransform == null)
        {
            spawnAnimation = null;
            yield break;
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * 0.85f;

        float elapsed = 0f;
        const float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            canvasGroup.alpha = t;
            rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one;
        spawnAnimation = null;
    }

    private IEnumerator RemoveRoutine()
    {
        if (canvasGroup == null || rectTransform == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = rectTransform.localScale;
        float elapsed = 0f;
        const float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            rectTransform.localScale = Vector3.Lerp(startScale, startScale * 0.85f, t);
            yield return null;
        }

        Destroy(gameObject);
    }
    private void SetFrame(bool framed,bool goldFrame, int type)
    {
        frame.gameObject.SetActive(framed);
        if (goldFrame)
        {
            frame.sprite = Resources.Load<Sprite>($"STS/Icons/Status{3+type*2}");
        }
        else
        {
            frame.sprite = Resources.Load<Sprite>($"STS/Icons/Status{3+type*2-1}");
        }
    }
}