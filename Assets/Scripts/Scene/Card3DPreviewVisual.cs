using UnityEngine;
using UnityEngine.UI;

public class Card3DPreviewVisual : MonoBehaviour
{
    [SerializeField] private Image cardFaceImage;
    [SerializeField] private SpriteRenderer cardFaceSpriteRenderer;
    [SerializeField] private GameObject[] rarityEffectRoots;

    public void SetSprite(Sprite sprite)
    {
        if (cardFaceImage != null)
            cardFaceImage.sprite = sprite;

        if (cardFaceSpriteRenderer != null)
            cardFaceSpriteRenderer.sprite = sprite;
    }

    public void ApplyRarity(int rarity)
    {
        if (rarityEffectRoots == null || rarityEffectRoots.Length == 0)
            return;

        for (int i = 0; i < rarityEffectRoots.Length; i++)
        {
            if (rarityEffectRoots[i] != null)
                rarityEffectRoots[i].SetActive(i == rarity);
        }
    }
}
