using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
public class CardReveal:MonoBehaviour
{
    public Transform cardRoot;
    public Image cardImage;
    public Image backImage;
    public Image effectImage;
    private Vector2 originalPosition;
    public bool revealed = false;
    public bool endFlip = false;
    public ParticleSystem shineParticles;
    public ParticleSystem revealParticles;
    private int rarity  = 0;
    public void SetRarity(int rarity)
    {
        this.rarity = rarity;
        if (rarity == 4)
        {
            shineParticles.gameObject.SetActive(true);
            shineParticles.Play();
        }
    }
    public void forceEndFlip()
    {
        endFlip = true;
        cardRoot.localRotation = Quaternion.identity;
        StopAllCoroutines();
        Debug.Log("Force end flip");
    }
    public void HideCard()
    {
        revealed = false;
        cardImage.gameObject.SetActive(false);
        backImage.gameObject.SetActive(false);
        effectImage.gameObject.SetActive(false);
        revealParticles.gameObject.SetActive(false);
        shineParticles.gameObject.SetActive(false);

        cardRoot.localRotation = Quaternion.identity;
        cardRoot.localScale = Vector3.one*0.6f;
    }
    void Awake()
    {
        SetFaceDown();
    }
    public void MemorizeFaceDown(Vector2 targetPos)
    {
        originalPosition = targetPos;
    }
    public void RevealCard()
    {
        revealParticles.transform.localScale*=0.5f;
        gameObject.SetActive(true);
        cardImage.gameObject.SetActive(true);
        backImage.gameObject.SetActive(false);
        effectImage.gameObject.SetActive(true);
        effectImage.sprite = cardImage.sprite;
        StartCoroutine(RevealCardRoutine());
    }
    public void SetFaceDown()
    {
        revealed = false;
        cardImage.gameObject.SetActive(false);
        backImage.gameObject.SetActive(true);
        effectImage.gameObject.SetActive(false);

        cardRoot.localRotation = Quaternion.identity;
        cardRoot.localScale = Vector3.one*0.6f;
    }
    public async Task Reveal()
    {
        if (revealed) return;
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // bring to front
        transform.localPosition = Vector3.zero;
        cardRoot.localScale = Vector3.one*1.5f;
        revealed = true;

        await FlipRoutine();
        StartCoroutine(RevealCardRoutine());
    }
    public void endReveal()
    {
        revealed = true;
        cardImage.gameObject.SetActive(true);
        backImage.gameObject.SetActive(false);
        cardRoot.localRotation = Quaternion.identity;
        cardRoot.GetComponent<RectTransform>().anchoredPosition = originalPosition;
        cardRoot.localScale = Vector3.one*0.6f;
    }
    private async Task FlipRoutine()
    {
        float duration = 0.25f*(0.3f+0.3f*rarity);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Lerp(0f, 90f, t);
            cardRoot.localRotation = Quaternion.Euler(0f, angle, 0f);
            await Task.Yield();
        }

        backImage.gameObject.SetActive(false);
        cardImage.gameObject.SetActive(true);
        effectImage.sprite = cardImage.sprite;
        effectImage.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Lerp(90f, 0f, t);
            cardRoot.localRotation = Quaternion.Euler(0f, angle, 0f);
            await Task.Yield();
            if (endFlip)
            {
                cardRoot.localRotation = Quaternion.identity;
                return;
            }
        }

        cardRoot.localRotation = Quaternion.identity;
    }
    public IEnumerator RevealCardRoutine()
    {
        if (rarity>1)
        {
            revealParticles.gameObject.SetActive(true);
            var trails = revealParticles.trails;
            trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(CardDatabase.Instance.GetRarityColor(rarity));
            var main = revealParticles.main;
            main.startLifetime = 0.5f+0.3f*rarity;
            main.startSize = 0.5f+0.3f*rarity;
            main.startSpeed = 100f/(0.5f+0.3f*rarity);
            revealParticles.Play();
        }
        if (rarity>0)
        {
            // Simple reveal animation
            float elapsed = 0f;
            float duration=0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                effectImage.color = new Color(1f, 1f, 1f,Mathf.Lerp(1f, 0f, t));
                effectImage.transform.localScale = (1.5f-0.5f*Mathf.Lerp(1f, 0f, t))*Vector3.one;
                yield return null;
            }
        }
    }
}