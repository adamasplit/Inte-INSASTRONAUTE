using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public CombatManager combat;
    public Character target;

    public Image highlight;
    public Image image;

    public bool isHovered = false;

    public bool acceptsEnemyCards = false;
    public static Character hoveredCharacter;
    bool deathAnimationPlayed;
    bool enemyImageSizeCached;
    Vector2 enemyImageSize;
    Texture2D deathDisintegrationTexture;
    Sprite deathDisintegrationSprite;
    Color32[] deathSourcePixels;
    Color32[] deathWorkingPixels;
    int deathTextureWidth;
    int deathTextureHeight;
    int deathNoiseSeed;
    LayoutElement deathSizeLock;
    bool deathSizeLockCreated;
    bool deathSizeLockOriginalIgnoreLayout;

    public void Init(CombatManager cm, Character t, bool acceptsEnemy)
    {
        combat = cm;
        target = t;
        CleanupDeathDisintegrationTexture();

        Sprite sprite = null;
        if (!target.isPlayer&&((Enemy)target).data!=null)
        {
            if (((Enemy)target).data.enemyName == "Mime")
            {
                sprite = Resources.Load<Sprite>("STS/Characters/" + RunManager.Instance.player.name);
            }
            else
            {
                sprite = Resources.Load<Sprite>("STS/Characters/" + ((Enemy)target).data.enemyName);
            }
        }
        else
        {
            sprite = Resources.Load<Sprite>("STS/Characters/" + target.name);
        }
        if (sprite != null)
        {
            image.gameObject.SetActive(true);
            image.sprite = sprite;
        }
        else
        {
            image.gameObject.SetActive(false);
        }

        if (image != null)
        {
            RectTransform imageRect = image.rectTransform;
            imageRect.localScale = Vector3.one;
            if (!target.isPlayer)
            {
                imageRect.anchoredPosition = Vector2.zero;
            }

            Color color = image.color;
            color.a = 1f;
            image.color = color;
        }

        enemyImageSizeCached = false;
        acceptsEnemyCards = acceptsEnemy;
        deathAnimationPlayed = false;

        highlight.color = acceptsEnemy ? new Color(1, 0, 0, 0f) : new Color(0, 1, 0, 0f);

        isHovered = false;
    }

    void OnDestroy()
    {
        CleanupDeathDisintegrationTexture();
    }

    void Update()
    {
        if (image == null)
            return;

        if (deathAnimationPlayed)
            return;

        RectTransform imageRect = image.rectTransform;

        if (target.isPlayer)
        {
            imageRect.sizeDelta = new Vector2(800, 800);
            return;
        }

        if (!enemyImageSizeCached)
        {
            RectTransform zoneRect = GetComponent<RectTransform>();
            float targetSize = Mathf.Min(zoneRect.rect.width, zoneRect.rect.height, 500f);
            if (targetSize <= 0f)
            {
                targetSize = Mathf.Min(imageRect.sizeDelta.x, imageRect.sizeDelta.y, 500f);
            }

            if (targetSize <= 0f)
            {
                targetSize = 500f;
            }

            enemyImageSize = new Vector2(targetSize, targetSize);
            enemyImageSizeCached = true;
        }

        imageRect.sizeDelta = enemyImageSize;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsValidTarget(eventData))
            return;

        SetHighlight(true);
        hoveredCharacter = target;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
        hoveredCharacter = null;
        isHovered = false;
    }

    public void SetHighlight(bool highlight)
    {
        this.highlight.color = highlight
            ? (acceptsEnemyCards ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f))
            : (acceptsEnemyCards ? new Color(1, 0, 0, 0f) : new Color(0, 1, 0, 0f));
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!isHovered)
            return;

        isHovered = false;

        var drag = eventData.pointerDrag?.GetComponentInParent<CardDrag>();
        drag.Destroy();
        var cardView = drag?.GetComponentInChildren<CardView>();

        if (cardView?.cardInstance == null)
        {
            Debug.LogWarning("Invalid drop");
            return;
        }

        var mode = cardView.cardInstance.targetingMode;
        var targets = combat.GetDisplayTargets(mode, target);

        if (targets.Count == 0)
            return;

        Vector2 discardPos = combat.animator.animationLayer.InverseTransformPoint(combat.ui.discardAnchor.position);
        combat.PlayCard(combat.player, cardView.cardInstance, targets);
    }

    bool IsValidTarget(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return false;

        var cardView = eventData.pointerDrag
            .GetComponent<CardDrag>()?
            .GetComponentInChildren<CardView>();

        if (cardView?.cardInstance?.data == null)
            return false;

        if (acceptsEnemyCards)
            return cardView.cardInstance.targetingMode == TargetingMode.Enemy ||
                   cardView.cardInstance.targetingMode == TargetingMode.AllEnemies ||
                   cardView.cardInstance.targetingMode == TargetingMode.AllCharacters ||
                   cardView.cardInstance.targetingMode == TargetingMode.RandomEnemy;

        return cardView.cardInstance.targetingMode == TargetingMode.Player ||
               cardView.cardInstance.targetingMode == TargetingMode.AllCharacters;
    }

    public IEnumerator FlashWhite()
    {
        Color originalColor = image.color;
        RectTransform imageRect = image.rectTransform;
        Vector2 originalPosition = imageRect.anchoredPosition;
        float moveDistance = 50f;
        Vector2 targetPosition = originalPosition + (target != null && target.isPlayer ? Vector2.zero : Vector2.down) * moveDistance;
        float duration = 0.2f;
        float punchDuration = duration * 0.35f;

        image.color = Color.white;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float colorT = Mathf.Clamp01(elapsed / duration);
            image.color = Color.Lerp(Color.white, originalColor, colorT);

            float positionT;
            if (elapsed <= punchDuration)
            {
                positionT = Mathf.Clamp01(elapsed / punchDuration);
                imageRect.anchoredPosition = Vector2.Lerp(originalPosition, targetPosition, positionT);
            }
            else
            {
                positionT = Mathf.Clamp01((elapsed - punchDuration) / (duration - punchDuration));
                imageRect.anchoredPosition = Vector2.Lerp(targetPosition, originalPosition, positionT);
            }

            yield return null;
        }

        image.color = originalColor;
        imageRect.anchoredPosition = originalPosition;
    }

    public IEnumerator PlayDeathAnimation(float duration = 0.65f)
    {
        if (deathAnimationPlayed || image == null || !image.gameObject.activeInHierarchy)
            yield break;
        if (!target.isPlayer)
        {
            if (RunManager.Instance!=null&&RunManager.Instance.eliteEncounter)
            {
                SFXManager.Instance.PlaySound("EliteDeath");
                duration*=2;
            }
            else
            {
                SFXManager.Instance.PlaySound("EnemyDeath");
            }
        }

        deathAnimationPlayed = true;

        bool useFallbackAnimation = Application.platform == RuntimePlatform.WebGLPlayer;
        Debug.Log($"[DropZone] PlayDeathAnimation: platform={Application.platform}, useFallback={useFallbackAnimation}");

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(image.rectTransform);
        Canvas.ForceUpdateCanvases();
        RectTransform imageRect = image.rectTransform;
        Vector2 originalPosition = imageRect.anchoredPosition;
        Vector2 lockedSize = imageRect.rect.size;
        bool originalPreserveAspect = image.preserveAspect;
        LockDeathAnimationSize(lockedSize);

        bool textureCreated = false;
        if (!useFallbackAnimation)
        {
            textureCreated = EnsureDeathDisintegrationTexture();
            Debug.Log($"[DropZone] EnsureDeathDisintegrationTexture result: {textureCreated}");
        }
        
        if (useFallbackAnimation || !textureCreated)
        {
            Debug.Log($"[DropZone] Using fallback death animation (fallback={useFallbackAnimation}, textureFailed={!textureCreated})");
            UnlockDeathAnimationSize();
            yield return PlayFallbackDeathAnimation(duration);
            yield break;
        }

        bool originalRaycastTarget = image.raycastTarget;
        Color originalColor = image.color;
        image.sprite = deathDisintegrationSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = originalPreserveAspect;
        image.color = originalColor.a > 0f ? Color.white : new Color(1f, 1f, 1f, 1f);
        image.raycastTarget = false;
        ApplyLockedDeathAnimationSize(imageRect, lockedSize);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fadeT = Mathf.SmoothStep(0f, 1f, t);

            ApplyDeathDisintegrationFrame(t, fadeT);

            ApplyLockedDeathAnimationSize(imageRect, lockedSize);
            imageRect.anchoredPosition = originalPosition + new Vector2(0f, -18f * fadeT);

            yield return null;
        }

        ApplyLockedDeathAnimationSize(imageRect, lockedSize);
        imageRect.anchoredPosition = originalPosition + new Vector2(0f, -18f);
        ApplyDeathDisintegrationFrame(1f, 1f);
        image.raycastTarget = originalRaycastTarget;
        image.color = Color.white;
        image.preserveAspect = originalPreserveAspect;
        UnlockDeathAnimationSize();
    }

    IEnumerator PlayFallbackDeathAnimation(float duration)
    {
        RectTransform imageRect = image.rectTransform;
        Vector2 originalPosition = imageRect.anchoredPosition;
        Color originalColor = image.color;

        float elapsed = 0f;
        float flashDuration = Mathf.Min(0.18f, duration * 0.35f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fadeT = Mathf.SmoothStep(0f, 1f, t);
            float flashT = flashDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / flashDuration);
            float flash = flashT < 0.5f ? flashT * 2f : (1f - flashT) * 2f;

            imageRect.anchoredPosition = originalPosition + new Vector2(0f, -24f * fadeT);

            Color tinted = Color.Lerp(originalColor, Color.white, flash * 0.75f);
            tinted.a = originalColor.a * (1f - fadeT);
            image.color = tinted;

            yield return null;
        }

        imageRect.anchoredPosition = originalPosition + new Vector2(0f, -24f);
        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

    bool EnsureDeathDisintegrationTexture()
    {
        if (image.sprite == null || image.sprite.texture == null)
            return false;

        CleanupDeathDisintegrationTexture();

        Sprite sourceSprite = image.sprite;
        Rect spriteRect = sourceSprite.textureRect;
        Rect sourceRect = sourceSprite.rect;

        deathTextureWidth = Mathf.Max(1, Mathf.RoundToInt(sourceRect.width));
        deathTextureHeight = Mathf.Max(1, Mathf.RoundToInt(sourceRect.height));
        deathNoiseSeed = sourceSprite.name.GetHashCode() ^ deathTextureWidth ^ (deathTextureHeight << 8);

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(sourceSprite.texture.width, sourceSprite.texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

        try
        {
            Graphics.Blit(sourceSprite.texture, temporary);
            RenderTexture.active = temporary;

            Texture2D readableTexture = new Texture2D(Mathf.Max(1, Mathf.RoundToInt(spriteRect.width)), Mathf.Max(1, Mathf.RoundToInt(spriteRect.height)), TextureFormat.RGBA32, false);
            readableTexture.filterMode = FilterMode.Point;
            readableTexture.ReadPixels(spriteRect, 0, 0);
            readableTexture.Apply(false, false);

            deathDisintegrationTexture = new Texture2D(deathTextureWidth, deathTextureHeight, TextureFormat.RGBA32, false);
            deathDisintegrationTexture.filterMode = FilterMode.Point;
            deathDisintegrationTexture.wrapMode = TextureWrapMode.Clamp;

            Vector2Int textureOffset = Vector2Int.RoundToInt(sourceSprite.textureRectOffset);
            textureOffset.x = Mathf.Clamp(textureOffset.x, 0, Mathf.Max(0, deathTextureWidth - readableTexture.width));
            textureOffset.y = Mathf.Clamp(textureOffset.y, 0, Mathf.Max(0, deathTextureHeight - readableTexture.height));

            Color32[] finalPixels = new Color32[deathTextureWidth * deathTextureHeight];
            Color32[] readablePixels = readableTexture.GetPixels32();

            if (sourceSprite.packed && sourceSprite.packingRotation != SpritePackingRotation.None)
            {
                readablePixels = UnpackPackedSpritePixels(readablePixels, readableTexture.width, readableTexture.height, sourceSprite.packingRotation);
            }

            for (int y = 0; y < readableTexture.height; y++)
            {
                int sourceRow = y * readableTexture.width;
                int destinationRow = (y + textureOffset.y) * deathTextureWidth;
                for (int x = 0; x < readableTexture.width; x++)
                {
                    finalPixels[destinationRow + textureOffset.x + x] = readablePixels[sourceRow + x];
                }
            }

            bool hasTrimmedPadding = textureOffset.x > 0 || textureOffset.y > 0 ||
                                     readableTexture.width != deathTextureWidth || readableTexture.height != deathTextureHeight;
            if (hasTrimmedPadding)
            {
                ClearDissolveBorder(finalPixels, deathTextureWidth, deathTextureHeight, textureOffset.x, textureOffset.y, readableTexture.width, readableTexture.height);
            }

            deathDisintegrationTexture.SetPixels32(finalPixels);
            deathDisintegrationTexture.Apply(false, false);

            deathSourcePixels = finalPixels;
            deathWorkingPixels = new Color32[deathSourcePixels.Length];

            Vector2 pivot = new Vector2(
                sourceRect.width <= 0f ? 0.5f : sourceSprite.pivot.x / sourceRect.width,
                sourceRect.height <= 0f ? 0.5f : sourceSprite.pivot.y / sourceRect.height);

            deathDisintegrationSprite = Sprite.Create(
                deathDisintegrationTexture,
                new Rect(0f, 0f, deathTextureWidth, deathTextureHeight),
                pivot,
                sourceSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                sourceSprite.border);

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to prepare death dissolve for {name}: {ex.Message}");
            CleanupDeathDisintegrationTexture();
            return false;
        }
        finally
        {
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    static Color32[] UnpackPackedSpritePixels(Color32[] sourcePixels, int width, int height, SpritePackingRotation packingRotation)
    {
        if (sourcePixels == null || sourcePixels.Length == 0)
            return sourcePixels;

        Color32[] unpackedPixels = new Color32[sourcePixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sourceX = x;
                int sourceY = y;

                switch (packingRotation)
                {
                    case SpritePackingRotation.FlipHorizontal:
                        sourceX = width - 1 - x;
                        break;
                    case SpritePackingRotation.FlipVertical:
                        sourceY = height - 1 - y;
                        break;
                    case SpritePackingRotation.Rotate180:
                        sourceX = width - 1 - x;
                        sourceY = height - 1 - y;
                        break;
                }

                unpackedPixels[y * width + x] = sourcePixels[sourceY * width + sourceX];
            }
        }

        return unpackedPixels;
    }

    void ApplyDeathDisintegrationFrame(float progress, float fadeT)
    {
        if (deathDisintegrationTexture == null || deathSourcePixels == null || deathWorkingPixels == null)
            return;

        System.Array.Clear(deathWorkingPixels, 0, deathWorkingPixels.Length);

        for (int y = 0; y < deathTextureHeight; y++)
        {
            float rowBias = deathTextureHeight <= 1 ? 0f : 1f - (y / (float)(deathTextureHeight - 1));
            float bottomBias = Mathf.SmoothStep(0f, 1f, rowBias);
            float topBias = 1f - bottomBias;
            float rowStart = Mathf.Lerp(0.6f, 0.0f, bottomBias);
            float rowSpan = Mathf.Lerp(0.38f, 0.55f, bottomBias);

            for (int x = 0; x < deathTextureWidth; x++)
            {
                int index = y * deathTextureWidth + x;
                Color32 source = deathSourcePixels[index];
                float pixelNoise = StableDeathNoise01(x, y, deathNoiseSeed, Mathf.FloorToInt(progress * 24f));

                if ((x == deathTextureWidth - 1 || y == deathTextureHeight - 1) && source.a < 250)
                {
                    continue;
                }

                float localProgress = Mathf.Clamp01((progress - rowStart) / Mathf.Max(0.0001f, rowSpan));
                float dissolveT = Mathf.SmoothStep(0f, 1f, localProgress);
                float visibility = Mathf.Clamp01(source.a / 255f) * (1f - dissolveT);

                float pixelFade = Mathf.Clamp01(1f - (pixelNoise - 0.5f) * 0.22f);
                visibility *= pixelFade;

                if (visibility <= 0f)
                    continue;

                byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * visibility), 0, 255);

                float redAmount = Mathf.Clamp01((1f - visibility) * 0.35f + fadeT * 0.08f);
                byte r = BlendDeathChannel(source.r, 150, redAmount);
                byte g = BlendDeathChannel(source.g, 14, redAmount);
                byte b = BlendDeathChannel(source.b, 10, redAmount * 0.85f);

                float alphaFactor = alpha / 255f;
                float colorFade = Mathf.Lerp(0.15f, 1f, alphaFactor);
                r = (byte)Mathf.Clamp(Mathf.RoundToInt(r * colorFade), 0, 255);
                g = (byte)Mathf.Clamp(Mathf.RoundToInt(g * colorFade), 0, 255);
                b = (byte)Mathf.Clamp(Mathf.RoundToInt(b * colorFade), 0, 255);

                deathWorkingPixels[index] = new Color32(r, g, b, alpha);
            }
        }

        deathDisintegrationTexture.SetPixels32(deathWorkingPixels);
        deathDisintegrationTexture.Apply(false, false);
        image.color = Color.white;
    }

    static byte BlendDeathChannel(byte source, byte target, float amount)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(source, target, Mathf.Clamp01(amount))), 0, 255);
    }

    static float StableDeathNoise01(int x, int y, int seed, int frame)
    {
        unchecked
        {
            int value = seed;
            value = (value * 397) ^ x;
            value = (value * 397) ^ y;
            value = (value * 397) ^ frame;
            value ^= value >> 13;
            value *= 1274126177;
            value ^= value >> 16;
            return (value & int.MaxValue) / (float)int.MaxValue;
        }
    }

    static void ClearDissolveBorder(Color32[] pixels, int textureWidth, int textureHeight, int offsetX, int offsetY, int width, int height)
    {
        if (pixels == null || pixels.Length == 0)
            return;

        int minX = Mathf.Clamp(offsetX, 0, textureWidth - 1);
        int minY = Mathf.Clamp(offsetY, 0, textureHeight - 1);
        int maxX = Mathf.Clamp(offsetX + width - 1, 0, textureWidth - 1);
        int maxY = Mathf.Clamp(offsetY + height - 1, 0, textureHeight - 1);

        for (int x = minX; x <= maxX; x++)
        {
            pixels[minY * textureWidth + x] = new Color32(0, 0, 0, 0);
            pixels[maxY * textureWidth + x] = new Color32(0, 0, 0, 0);
        }

        for (int y = minY; y <= maxY; y++)
        {
            pixels[y * textureWidth + minX] = new Color32(0, 0, 0, 0);
            pixels[y * textureWidth + maxX] = new Color32(0, 0, 0, 0);
        }
    }

    void CleanupDeathDisintegrationTexture()
    {
        if (image != null && deathDisintegrationSprite != null && image.sprite == deathDisintegrationSprite)
        {
            image.sprite = null;
        }

        if (deathDisintegrationSprite != null)
        {
            Destroy(deathDisintegrationSprite);
            deathDisintegrationSprite = null;
        }

        if (deathDisintegrationTexture != null)
        {
            Destroy(deathDisintegrationTexture);
            deathDisintegrationTexture = null;
        }

        deathSourcePixels = null;
        deathWorkingPixels = null;
        deathTextureWidth = 0;
        deathTextureHeight = 0;
    }

    void LockDeathAnimationSize(Vector2 lockedSize)
    {
        if (image == null)
            return;

        if (deathSizeLock == null)
        {
            deathSizeLock = image.GetComponent<LayoutElement>();
            if (deathSizeLock == null)
            {
                deathSizeLock = image.gameObject.AddComponent<LayoutElement>();
                deathSizeLockCreated = true;
            }
        }

        deathSizeLock.minWidth = lockedSize.x;
        deathSizeLock.minHeight = lockedSize.y;
        deathSizeLock.preferredWidth = lockedSize.x;
        deathSizeLock.preferredHeight = lockedSize.y;
        deathSizeLock.flexibleWidth = 0f;
        deathSizeLock.flexibleHeight = 0f;
        deathSizeLockOriginalIgnoreLayout = deathSizeLock.ignoreLayout;
        deathSizeLock.ignoreLayout = true;
        Canvas.ForceUpdateCanvases();
    }

    void ApplyLockedDeathAnimationSize(RectTransform imageRect, Vector2 lockedSize)
    {
        if (imageRect == null)
            return;

        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, lockedSize.x);
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lockedSize.y);
    }

    void UnlockDeathAnimationSize()
    {
        if (deathSizeLock == null)
            return;

        if (deathSizeLockCreated)
        {
            Destroy(deathSizeLock);
        }
        else
        {
            deathSizeLock.minWidth = -1f;
            deathSizeLock.minHeight = -1f;
            deathSizeLock.preferredWidth = -1f;
            deathSizeLock.preferredHeight = -1f;
            deathSizeLock.flexibleWidth = -1f;
            deathSizeLock.flexibleHeight = -1f;
            deathSizeLock.ignoreLayout = deathSizeLockOriginalIgnoreLayout;
        }

        deathSizeLock = null;
        deathSizeLockCreated = false;
        Canvas.ForceUpdateCanvases();
    }
}