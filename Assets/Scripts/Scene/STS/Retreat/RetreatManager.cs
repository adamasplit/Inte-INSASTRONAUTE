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
    private bool retireRequestStarted;
    private Task retireRequestTask;
    private STSApiRunRetireResponse retireResponse;
    private bool previewRequestStarted;
    private Task previewRequestTask;
    private STSApiRetreatPreviewResponse retreatPreviewResponse;

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
        bool finalActCompleted = RunManager.Instance != null && RunManager.Instance.completedFinalAct;
        choicePanel.SetActive(!finalActCompleted);
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

        STSRunAuditSystem.RecordNodeEntered(RunManager.Instance, RunManager.Instance.currentNode, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "retreat_init");

        if (finalActCompleted)
        {
            Begin();
        }

        STSSceneLoader.Instance?.EndLoading();
        STSSceneLoader.Instance?.SceneReady();
    }

    void Begin()
    {
        scoreResult = CalculateRunScore();
        RenderScore(scoreResult);
        if (RunManager.Instance != null && RunManager.Instance.completedFinalAct)
        {
            _ = EnsureFinalActRetireAppliedAsync();
        }
        else
        {
            _ = EnsureRetreatPreviewLoadedAsync();
            _ = EnsureTokenRewardAppliedAsync();
        }

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
        if (RunManager.Instance != null && RunManager.Instance.completedFinalAct)
        {
            await EnsureFinalActRetireAppliedAsync();
        }
        else
        {
            await EnsureTokenRewardAppliedAsync();
        }

        if (RunManager.Instance != null && RunManager.Instance.completedFinalAct)
        {
            STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Boot", "final_act_continue");
            RunManager.Instance.OnRunEnd(true, false);
            STSSceneLoader.Instance.LoadScene("STS_Boot");
            return;
        }

        STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Map", "retreat_continue");
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

        if (RunManager.Instance != null && RunManager.Instance.completedFinalAct)
        {
            await EnsureFinalActRetireAppliedAsync();
        }
        else
        {
            await EnsureTokenRewardAppliedAsync();
        }
        STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Boot", "retreat_menu");
        RunManager.Instance.OnRunEnd(true, !(RunManager.Instance != null && RunManager.Instance.completedFinalAct));
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
            scoreTitleText.text = RunManager.Instance != null && RunManager.Instance.completedFinalAct
                ? "Dernier acte terminé"
                : "Score de la partie";
        }

        if (scoreDetailsText != null)
        {
            scoreDetailsText.text = RunManager.Instance != null && RunManager.Instance.completedFinalAct
                ? "Vous avez fini le dernier acte. Pour aller plus loin, revenez la semaine prochaine !\n\n" + breakdownText
                : breakdownText;
        }

        if (tokenRewardText != null)
        {
            tokenRewardText.text = $"Récompenses en tokens: +{result.tokenReward}";
        }

        Debug.Log($"Retreat score details:\n{breakdownText}\nToken reward: +{result.tokenReward}");
    }

    private async Task EnsureTokenRewardAppliedAsync()
    {
        if (RunManager.Instance != null && RunManager.Instance.completedFinalAct)
        {
            await EnsureFinalActRetireAppliedAsync();
            return;
        }

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

        await EnsureRetreatPreviewLoadedAsync();

        long tokens = retreatPreviewResponse != null && retreatPreviewResponse.accepted
            ? Math.Max(0, retreatPreviewResponse.tokensPreview)
            : Mathf.Max(0, scoreResult.tokenReward);

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

    private async Task EnsureRetreatPreviewLoadedAsync()
    {
        if (RunManager.Instance == null
            || RunManager.Instance.completedFinalAct
            || string.IsNullOrWhiteSpace(RunManager.Instance.runId)
            || RunManager.Instance.unrestrictedMode)
        {
            return;
        }

        if (previewRequestStarted)
        {
            if (previewRequestTask != null)
            {
                await previewRequestTask;
            }
            return;
        }

        previewRequestStarted = true;
        previewRequestTask = LoadRetreatPreviewAsync();
        await previewRequestTask;
    }

    private async Task LoadRetreatPreviewAsync()
    {
        try
        {
            retreatPreviewResponse = await STSApiClient.RetreatPreviewAsync(RunManager.Instance.runId);
            if (retreatPreviewResponse == null || !retreatPreviewResponse.accepted)
            {
                Debug.LogWarning("Retreat preview request did not return an accepted response. Keeping local retreat summary.");
                return;
            }

            RunManager.Instance.apiStatus = retreatPreviewResponse.status;
            RenderServerPreviewSummary(retreatPreviewResponse);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load retreat preview through API, keeping local summary: {ex.Message}");
        }
    }

    private async Task EnsureFinalActRetireAppliedAsync()
    {
        if (retireRequestStarted)
        {
            if (retireRequestTask != null)
            {
                await retireRequestTask;
            }
            return;
        }

        retireRequestStarted = true;
        retireRequestTask = ApplyFinalActRetireAsync();
        await retireRequestTask;
    }

    private async Task ApplyFinalActRetireAsync()
    {
        if (RunManager.Instance == null || string.IsNullOrWhiteSpace(RunManager.Instance.runId) || RunManager.Instance.unrestrictedMode)
            return;

        try
        {
            retireResponse = await STSApiClient.RetireRunAsync(RunManager.Instance.runId);
            if (retireResponse == null || !retireResponse.accepted)
            {
                Debug.LogWarning("Final-act retire request did not return an accepted response. Falling back to local score display.");
                return;
            }

            RunManager.Instance.apiStatus = retireResponse.status;
            RenderServerRetireSummary(retireResponse);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to retire run through API, keeping local summary as fallback: {ex.Message}");
        }
    }

    private void RenderServerRetireSummary(STSApiRunRetireResponse response)
    {
        if (response == null)
            return;

        if (scoreTitleText != null)
        {
            scoreTitleText.text = "Dernier acte terminé";
        }

        if (scoreDetailsText != null)
        {
            scoreDetailsText.text = BuildServerBreakdownText(
                "Vous avez vaincu le boss final. Les recompenses de fin de run ont ete calculees par le serveur.",
                response.status,
                response.score,
                response.visitedNodeScore,
                response.combatVictoryScore,
                response.eliteVictoryScore,
                response.eventVisitedScore,
                response.actReachedScore,
                response.relicOwnedScore,
                response.deckCardScore,
                response.goldOwnedScore,
                response.remainingHpPercentScore,
                response.scorePerToken,
                response.rounding,
                response.minimumReward
            );
        }

        if (tokenRewardText != null)
        {
            tokenRewardText.text = $"Tokens accordes: +{response.tokensGranted} (solde: {response.tokenBalance})";
        }
    }

    private void RenderServerPreviewSummary(STSApiRetreatPreviewResponse response)
    {
        if (response == null)
            return;

        if (scoreTitleText != null)
        {
            scoreTitleText.text = "Score de la partie";
        }

        if (scoreDetailsText != null)
        {
            scoreDetailsText.text = BuildServerBreakdownText(
                "Apercu serveur pour cette fin d'acte (sans retrait de run).",
                response.status,
                response.score,
                response.visitedNodeScore,
                response.combatVictoryScore,
                response.eliteVictoryScore,
                response.eventVisitedScore,
                response.actReachedScore,
                response.relicOwnedScore,
                response.deckCardScore,
                response.goldOwnedScore,
                response.remainingHpPercentScore,
                response.scorePerToken,
                response.rounding,
                response.minimumReward
            );
        }

        if (tokenRewardText != null)
        {
            tokenRewardText.text = $"Tokens prevus: +{response.tokensPreview} (solde projete: {response.projectedTokenBalance})";
        }
    }

    private string BuildServerBreakdownText(
        string intro,
        string status,
        long score,
        long visitedNodeScore,
        long combatVictoryScore,
        long eliteVictoryScore,
        long eventVisitedScore,
        long actReachedScore,
        long relicOwnedScore,
        long deckCardScore,
        long goldOwnedScore,
        long remainingHpPercentScore,
        long scorePerToken,
        string rounding,
        long minimumReward
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine(intro);
        sb.AppendLine();
        sb.AppendLine($"Etages parcourus: +{visitedNodeScore}");
        sb.AppendLine($"Combats gagnes: +{combatVictoryScore}");
        sb.AppendLine($"Elites vaincues: +{eliteVictoryScore}");
        sb.AppendLine($"Evenements visites: +{eventVisitedScore}");
        sb.AppendLine($"Actes atteints: +{actReachedScore}");
        sb.AppendLine($"Reliques possedees: +{relicOwnedScore}");
        sb.AppendLine($"Taille du deck: +{deckCardScore}");
        sb.AppendLine($"Or transporte: +{goldOwnedScore}");
        sb.AppendLine($"PV restants: +{remainingHpPercentScore}");
        sb.AppendLine();
        sb.AppendLine($"Score serveur: {score}");
        sb.AppendLine($"Statut run: {status}");
        if (scorePerToken > 0)
        {
            sb.AppendLine($"Conversion: {rounding} ({scorePerToken} score par token, minimum {minimumReward})");
        }

        return sb.ToString();
    }

    private async Task<bool> TryGrantTokensWithReflectionAsync(long amount)
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

    private static bool TryConvertNumeric(long value, Type targetType, out object convertedValue)
    {
        convertedValue = null;

        if (targetType == typeof(int))
        {
            if (value < int.MinValue || value > int.MaxValue)
                return false;
            convertedValue = (int)value;
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
