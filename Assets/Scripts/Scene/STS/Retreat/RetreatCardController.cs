using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class RetreatCardController : MonoBehaviour
{
    public CardView view;
    public GameObject glow;

    STSCardData data;
    bool revealed = false;
    public RectTransform root;
    public TextMeshProUGUI tokenText;
    private RetreatManager manager;
    public void Init(STSCardData card, RetreatManager manager)
    {
        data = card;
        this.manager = manager;

        view.Set(card);

        // cacher visuellement
        view.gameObject.SetActive(false);
        glow.SetActive(false);
    }

    public void Reveal()
    {
        if (revealed) return;
        revealed = true;

        view.gameObject.SetActive(true);

        StartCoroutine(TransformRoutine());
    }

    

    IEnumerator TransformRoutine()
    {
        // glow ON
        GetComponent<EnscaleVanish>().StartVanish();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TransformToReward());
    }

    IEnumerator TransformToReward()
    {
        view.gameObject.SetActive(false);
        if (data.collectionCard != null)
        {
            glow.GetComponent<Image>().sprite=data.collectionCard.sprite;
            glow.SetActive(true);
            manager.AddCardToDatas(data.collectionCard);
        }
        else
        {
            for (int i=0; i<200; i++)
            {
                tokenText.text = i.ToString();
                yield return new WaitForSeconds(0.01f);
            }
        }
    }
    public IEnumerator MoveFromTo(Vector2 source, float duration)
    {
        root.anchoredPosition = source;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Ease out cubic for smoother, less linear movement
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            root.anchoredPosition = Vector2.Lerp(source, Vector2.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        root.anchoredPosition = Vector2.zero;
    }
}