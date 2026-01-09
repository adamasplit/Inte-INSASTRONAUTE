using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] public Image cardImage;
    [SerializeField] private Image borderImage;

    public void SetCardData(int number, Sprite sprite, Color? borderColor = null)
    {
        if (numberText!=null)
            numberText.text = number.ToString();
        cardImage.sprite = sprite;

        if (borderColor.HasValue)
        {
            borderImage.color = borderColor.Value;
        }
    }
}