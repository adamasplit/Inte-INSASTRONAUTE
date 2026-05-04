using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
public class RewardManager : MonoBehaviour
{
    public Transform cardsContainer;
    public GameObject cardPrefab;
    public List<RewardCardController> cards = new();
    void Start()
    {
        Reward reward;
        if (RunManager.Instance!=null && RunManager.Instance.pendingReward != null)
        {
            reward = RunManager.Instance.pendingReward;
        }
        else
        {
            Debug.LogWarning("No pending reward found, generating default reward.");
            reward = new Reward
            {
                cardChoices = new RewardGenerator().GenerateCardChoices(new CombatResult())
            };
        }
        StartCoroutine(SpawnCardsAnimated(reward.cardChoices));
    }

    IEnumerator SpawnCardsAnimated(List<STSCardData> cards)
    {
        foreach (var card in cards)
        {
            var obj = Instantiate(cardPrefab, cardsContainer);

            var ctrl = obj.GetComponent<RewardCardController>();
            ctrl.Init(card, this);
            this.cards.Add(ctrl);
            StartCoroutine(AnimateCardIn(obj));

            yield return new WaitForSeconds(0.15f);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer as RectTransform);
    }

    IEnumerator AnimateCardIn(GameObject obj)
    {
        float t = 0;
        float duration = 1f;

        var rect = obj.transform.GetChild(0).GetComponent<RectTransform>();
        var cg = obj.GetComponent<CanvasGroup>();

        Vector3 startPos = rect.anchoredPosition + Vector2.up * 100;

        rect.localScale = Vector3.one * 0.8f;
        cg.alpha = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            rect.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, k);
            rect.anchoredPosition = Vector2.Lerp(startPos, rect.anchoredPosition, k);
            cg.alpha = k;

            yield return null;
        }

        rect.localScale = Vector3.one;
        cg.alpha = 1;
    }

    public void OnCardSelected(STSCardData card, RewardCardController selected)
    {
        
        StartCoroutine(HandleSelection(selected));
        if (RunManager.Instance != null)
        {
            RunManager.Instance.deck.Add(new CardInstance(card));
            RunManager.Instance.pendingReward = null;
        }
    }

    IEnumerator HandleSelection(RewardCardController selected)
    {
        foreach (var card in cards)
        {
            if (card != selected)
                StartCoroutine(FadeOut(card.gameObject));
        }
        RectTransform rt = selected.gameObject.transform as RectTransform;
        yield return StartCoroutine(ZoomToCenter(selected.gameObject));
    }

    public void OnRelicSelected(Relic relic)
    {
        RunManager.Instance.AddRelic(relic);

        RunManager.Instance.pendingReward = null;

    }

    public void EndReward()
    {
        RunManager.Instance.pendingReward = null;
        bool elite = RunManager.Instance.eliteEncounter;
        bool boss = RunManager.Instance.bossEncounter;
        RunManager.Instance.eliteEncounter = false;
        RunManager.Instance.bossEncounter = false;
        if (boss)
        {
            SceneManager.LoadScene("STS_Retreat");
        }
        else
        {
            SceneManager.LoadScene("STS_Map");
        }
    }

    IEnumerator FadeOut(GameObject obj)
    {
        float t = 0;
        float duration = 0.2f;

        var cg = obj.GetComponent<CanvasGroup>();

        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = 1 - (t / duration);
            yield return null;
        }
        cg.alpha = 0;
    }
    IEnumerator ZoomToCenter(GameObject obj)
    {
        float t = 0;
        float duration = 0.2f;

        var rect = obj.transform.GetChild(0) as RectTransform;
        var parentRect = rect.parent as RectTransform;
        var canvas = rect.GetComponentInParent<Canvas>();
        var uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3 startScale = rect.localScale;
        Vector3 endScale = Vector3.one * 1.5f;

        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = Vector2.zero;
        if (parentRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                uiCamera,
                out endPos
            );
        }
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            rect.localScale = Vector3.Lerp(startScale, endScale, k);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, k);

            yield return null;
        }
    }
}