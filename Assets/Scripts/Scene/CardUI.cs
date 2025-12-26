using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Image cardImage;
    [SerializeField] private Image borderImage;

    public void SetCardData(int number, string spriteName, Color? borderColor = null)
    {
        numberText.text = number.ToString();
        cardImage.sprite = Resources.Load<Sprite>("Sprites/" + spriteName);

        if (borderColor.HasValue)
        {
            borderImage.color = borderColor.Value;
        }
    }
}