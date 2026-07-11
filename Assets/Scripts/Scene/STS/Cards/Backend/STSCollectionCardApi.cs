using System.Threading.Tasks;
using UnityEngine;

public static class STSCollectionCardApi
{
    public static Task<Sprite> LoadSpriteAsync(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return Task.FromResult<Sprite>(null);

        Sprite sprite = Resources.Load<Sprite>($"Sprites/Cartes/{cardId}");
        if (sprite == null)
        {
            Debug.LogWarning($"Collection card sprite not found in Resources/Sprites/Cartes/{cardId}.");
        }

        return Task.FromResult(sprite);
    }
}