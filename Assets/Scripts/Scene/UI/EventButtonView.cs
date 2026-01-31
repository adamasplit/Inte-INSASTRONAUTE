using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventButtonView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image bannerImage;         // optionnel
    [SerializeField] private Button button;

    public void Bind(string title, string tag, Sprite banner, Action onClick)
    {
        if (titleText) titleText.text = title ?? "";

        if (bannerImage)
        {
            bannerImage.gameObject.SetActive(banner != null);
            if (banner != null) bannerImage.sprite = banner;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}
