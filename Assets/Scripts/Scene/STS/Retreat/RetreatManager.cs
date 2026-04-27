using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
public class RetreatManager : MonoBehaviour
{
    public Transform gridContainer;
    public GameObject cardPrefab;
    public ScrollRect scrollRect;
    public GameObject choicePanel;
    public CanvasGroup buttonCanvasGroup;
    private CardData[] cardsData;

    public float revealDelay = 0.2f;
    bool fastForward = false;

    List<RetreatCardController> cards = new();
    void Start()
    {
        choicePanel.SetActive(true);
        if (RunManager.Instance == null)
        {
            Debug.LogError("No RunManager instance found!");
            RunManager.Instance = new GameObject("RunManager").AddComponent<RunManager>();
            RunManager.Instance.StartRun("aa", 100, new List<Relic>(),false);
        }
    }
    void Begin()
    {
        buttonCanvasGroup.alpha = 0f;
        buttonCanvasGroup.interactable = false;
        StartCoroutine(RevealRoutine());

    }

    IEnumerator RevealRoutine()
    {
        int i = 0;
        List<STSCardData> cardsData = RunManager.Instance.deck;
        foreach (var card in cardsData)
        {
            i++;
            float delay = fastForward ? 0.02f : 0.2f;
            StartCoroutine(SpawnAndAnimate(card, delay*i));
        }
        yield return new WaitForSeconds(cardsData.Count*0.2f);
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            buttonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        buttonCanvasGroup.interactable = true;
    }


    IEnumerator SpawnAndAnimate(STSCardData card,float delay=0.2f)
    {
        var obj = Instantiate(cardPrefab, gridContainer);

        var ctrl = obj.GetComponent<RetreatCardController>();
        ctrl.Init(card, this);
        yield return new WaitForSeconds(delay);
        // IMPORTANT : attendre layout
        yield return null;
        ctrl.Reveal();
        StartCoroutine(ctrl.MoveFromTo(new Vector3(00f, -1000f, 0f), 0.2f));
    }

    public void OnFastForwardPressed()
    {
        fastForward = true;
    }

    public void OnContinuePressed()
    {
        RunManager.Instance.RegenerateMap = true;
        RunManager.Instance.lastActEndFloor= RunManager.Instance.currentFloor;
        SceneManager.LoadScene("STS_Map");
    }
    public void OnRetreatPressed()
    {
        Begin();
        choicePanel.SetActive(false);
    }
    public async void GoToMenu()
    {
        await AddObtainedCardsToCollection();
        SceneManager.LoadScene("STS_Boot");
    }

    public async Task AddObtainedCardsToCollection()
    {
        if (cardsData == null || cardsData.Length == 0)
            return;

        await PlayerProfileStore.AddCards(cardsData);
    }

    public void AddCardToDatas(CardData card)
    {
        if (cardsData == null)
        {
            cardsData = new[] { card };
            return;
        }

        System.Array.Resize(ref cardsData, cardsData.Length + 1);
        cardsData[cardsData.Length - 1] = card;
    }
}