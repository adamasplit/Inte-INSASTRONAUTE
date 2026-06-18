using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RetreatManager : MonoBehaviour
{
    public Transform gridContainer;
    public GameObject cardPrefab;
    public ScrollRect scrollRect;
    public GameObject choicePanel;
    public GameObject scorePanel;
    public CanvasGroup buttonCanvasGroup;
    public TextMeshProUGUI scoreTitleText;
    public TextMeshProUGUI scoreDetailsText;
    public TextMeshProUGUI tokenRewardText;

    [Header("Score To Token")]
    public int scorePerToken = 20;
    public int minimumTokenReward = 10;

    public float revealDelay = 0.2f;
    bool fastForward = false;

    private RunScoreResult scoreResult;
    private bool tokenGrantStarted;
    private Task tokenGrantTask;

    private class ScoreLine
    {
        public string label;
        public int value;
    }

    private class RunScoreResult
    {
        public readonly List<ScoreLine> lines = new();
        public int totalScore;
        public int tokenReward;
    }

    private static readonly string[] TokenMethodCandidates =
    {
        "AddTokens",
        "AddToken",
        "AddCurrency",
        "AddCoins",
        "AddSoftCurrency",
        "GrantTokens"
    };
    void Awake()
    {
        scoreTitleText.text = "";
        scoreDetailsText.text = "";
        tokenRewardText.text = "";
    }

    async void Start()
    {
        choicePanel.SetActive(true);
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }

        if (RunManager.Instance == null)
        {
            Debug.LogError("No RunManager instance found!");
            RunManager.Instance = new GameObject("RunManager").AddComponent<RunManager>();
            await RunManager.Instance.StartRunAsync("aa", 100, new List<Relic>(), false);
        }

        STSSceneLoader.Instance?.EndLoading();
        STSSceneLoader.Instance?.SceneReady();
    }

    void Begin()
    {
        scoreResult = CalculateRunScore();
        RenderScore(scoreResult);
        _ = EnsureTokenRewardAppliedAsync();

        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
        }

        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = 0f;
            buttonCanvasGroup.interactable = false;
            StartCoroutine(RevealRoutine());
        }
    }

    IEnumerator RevealRoutine()
    {
        yield return new WaitForSeconds(fastForward ? 0.05f : revealDelay);

        float elapsed = 0f;
        float fadeDuration = fastForward ? 0.2f : 1f;
        while (elapsed < 1f)
        {
            buttonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed);
            elapsed += Time.deltaTime / fadeDuration;
            yield return null;
        }

        buttonCanvasGroup.alpha = 1f;
        buttonCanvasGroup.interactable = true;
    }

    public void OnFastForwardPressed()
    {
        fastForward = true;
    }

    public async void OnContinuePressed()
    {
        await EnsureTokenRewardAppliedAsync();
        RunManager.Instance.RegenerateMap = true;
        RunManager.Instance.act++;
        STSSceneLoader.Instance.LoadScene("STS_Map");
    }

    public void OnRetreatPressed()
    {
        Begin();
        choicePanel.SetActive(false);
    }

    private bool goingToMenu = false;

    public async void GoToMenu()
    {
        if (goingToMenu) return;
        goingToMenu = true;

        await EnsureTokenRewardAppliedAsync();
        RunManager.Instance.OnRunEnd();
        STSSceneLoader.Instance.LoadScene("STS_Boot");
    }

    private RunScoreResult CalculateRunScore()
    {
        var result = new RunScoreResult();
        var run = RunManager.Instance;

        if (run == null)
        {
            result.lines.Add(new ScoreLine { label = "No active run", value = 0 });
            result.totalScore = 0;
            result.tokenReward = 0;
            return result;
        }

        int visitedNodes = run.map != null
            ? run.map.Count(n => n != null && n.visited)
            : Mathf.Max(0, run.currentFloor - 1);

        int combats = run.map != null
            ? run.map.Count(n => n != null && n.visited && n.type == NodeType.Combat)
            : 0;

        int elites = run.map != null
            ? run.map.Count(n => n != null && n.visited && n.type == NodeType.Elite)
            : 0;

        int eventsCount = run.map != null
            ? run.map.Count(n => n != null && n.visited && n.type == NodeType.Event)
            : 0;

        int deckCount = run.deck != null ? run.deck.Count : 0;
        int relicCount = run.relics != null ? run.relics.Count : 0;

        int hpPercent = 0;
        if (run.player != null && run.player.maxHP > 0)
        {
            hpPercent = Mathf.RoundToInt((float)run.player.currentHP / run.player.maxHP * 100f);
        }

        result.lines.Add(new ScoreLine { label = "Etages parcourus", value = visitedNodes * 15 });
        result.lines.Add(new ScoreLine { label = "Combats gagnés", value = combats * 25 });
        result.lines.Add(new ScoreLine { label = "Elites vaincues", value = elites * 60 });
        result.lines.Add(new ScoreLine { label = "Événements visités", value = eventsCount * 20 });
        result.lines.Add(new ScoreLine { label = "Actes atteints", value = run.act * 120 });
        result.lines.Add(new ScoreLine { label = "Reliques possédées", value = relicCount * 40 });
        result.lines.Add(new ScoreLine { label = "Taille du deck", value = deckCount * 5 });
        result.lines.Add(new ScoreLine { label = "Or transporté", value = Mathf.Max(0, run.gold) });
        result.lines.Add(new ScoreLine { label = "PV restants", value = hpPercent * 2 });

        result.totalScore = result.lines.Sum(l => l.value);
        int safeScorePerToken = Mathf.Max(1, scorePerToken);
        result.tokenReward = Mathf.Max(minimumTokenReward, Mathf.RoundToInt((float)result.totalScore / safeScorePerToken));

        return result;
    }

    private void RenderScore(RunScoreResult result)
    {
        if (result == null)
            return;

        var sb = new StringBuilder();
        foreach (var line in result.lines)
        {
            sb.Append(line.label);
            sb.Append(": +");
            sb.Append(line.value);
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.Append("Total score: ");
        sb.Append(result.totalScore);

        string breakdownText = sb.ToString();

        if (scoreTitleText != null)
        {
            scoreTitleText.text = "Score de la partie";
        }

        if (scoreDetailsText != null)
        {
            scoreDetailsText.text = breakdownText;
        }

        if (tokenRewardText != null)
        {
            tokenRewardText.text = $"Récompenses en tokens: +{result.tokenReward}";
        }

        Debug.Log($"Retreat score details:\n{breakdownText}\nToken reward: +{result.tokenReward}");
    }

    private async Task EnsureTokenRewardAppliedAsync()
    {
        if (tokenGrantStarted)
        {
            if (tokenGrantTask != null)
            {
                await tokenGrantTask;
            }
            return;
        }

        tokenGrantStarted = true;
        tokenGrantTask = ApplyTokenRewardAsync();
        await tokenGrantTask;
    }

    private async Task ApplyTokenRewardAsync()
    {
        if (scoreResult == null)
        {
            scoreResult = CalculateRunScore();
            RenderScore(scoreResult);
        }

        int tokens = Mathf.Max(0, scoreResult.tokenReward);
        if (tokens <= 0)
            return;

        try
        {
            bool granted = await TryGrantTokensWithReflectionAsync(tokens);
            if (!granted)
            {
                Debug.LogWarning($"Retreat token reward could not be applied. No compatible method found on {nameof(PlayerProfileStore)}.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while granting retreat token reward: {ex}");
        }
    }

    private async Task<bool> TryGrantTokensWithReflectionAsync(int amount)
    {
        Type storeType = typeof(PlayerProfileStore);

        foreach (string methodName in TokenMethodCandidates)
        {
            MethodInfo method = storeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1)
                continue;

            if (!TryConvertNumeric(amount, parameters[0].ParameterType, out object convertedValue))
                continue;

            object invokeResult = method.Invoke(null, new[] { convertedValue });
            if (invokeResult is Task invokeTask)
            {
                await invokeTask;
            }

            return true;
        }

        return false;
    }

    private static bool TryConvertNumeric(int value, Type targetType, out object convertedValue)
    {
        convertedValue = null;

        if (targetType == typeof(int))
        {
            convertedValue = value;
            return true;
        }

        if (targetType == typeof(long))
        {
            convertedValue = (long)value;
            return true;
        }

        if (targetType == typeof(float))
        {
            convertedValue = (float)value;
            return true;
        }

        if (targetType == typeof(double))
        {
            convertedValue = (double)value;
            return true;
        }

        return false;
    }

    public void AddCardToDatas(CardData card)
    {
        Debug.Log("Retreat no longer grants collection cards. Ignoring AddCardToDatas call.");
    }
}
