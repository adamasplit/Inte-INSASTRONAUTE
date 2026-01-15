using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardElement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image userIconImage;

    public void SetData(int rank, string playerName, int score, Sprite userIcon = null, Color? backgroundColor = null)
    {
        rankText.text = rank.ToString();
        nameText.text = playerName;
        scoreText.text = score.ToString();

        if (userIconImage!= null)
        {
            if (userIcon != null)
            {
                userIconImage.sprite = userIcon;
                userIconImage.gameObject.SetActive(true);
            }
            else
            {
                userIconImage.gameObject.SetActive(false);
            }
        }
            if (backgroundColor.HasValue)
            {
                backgroundImage.color = backgroundColor.Value;
            }
        
    }
}