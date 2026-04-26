using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class RewardManager : MonoBehaviour
{
    public Transform cardsContainer;
    public GameObject cardPrefab;

    void Start()
    {
        var reward = RunManager.Instance.pendingReward;

        foreach (var card in reward.cardChoices)
        {
            GameObject obj = Instantiate(cardPrefab, cardsContainer);

            RewardCardController ctrl = obj.GetComponent<RewardCardController>();
            ctrl.Init(card, this);
        }
    }

    public void OnCardSelected(STSCardData card)
    {
        RunManager.Instance.deck.Add(card);

        RunManager.Instance.pendingReward = null;

        SceneManager.LoadScene("STS_Map");
    }
}