using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] public Image cardImage;

    public void SetCardData(int number, Sprite sprite)
    {
        if (numberText!=null)
            numberText.text = number.ToString();
        cardImage.sprite = sprite;
    }
}