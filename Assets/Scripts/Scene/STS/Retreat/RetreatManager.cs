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
    private bool leavingRetreat;

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

    private static readonly string[] TokenBalanceMethodCandidates =
    {
        "GetTokens",
        "GetTokenBalance",
        "GetCurrency",
        "GetCoins",
        "GetSoftCurrency"
    };

    private static readonly string[] TokenBalanceMemberCandidates =
    {
        "Tokens",
        "TokenBalance",
        "Currency",
        "Coins",
        "SoftCurrency"
    };
    void Awake()
    {
        scoreTitleText.text = "";
        scoreDetailsText.text = "";
        tokenRewardText.text = "";
    }

    async void Start()
    {
        if (RunManager.Instance != null && RunManager.Instance.completedFinalAct && !EnemyPoolDatabase.IsLastAct(RunManager.Instance.act))
        {
            Debug.LogWarning($"[STS-RUN] completedFinalAct was true outside last act (act={RunManager.Instance.act}). Resetting flag for retreat flow.");
            RunManager.Instance.completedFinalAct = false;
        }

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
        if (leavingRetreat)
            return;

        leavingRetreat = true;
        STSSceneLoader.Instance?.BeginLoading();

        if (choicePanel != null)
            choicePanel.SetActive(false);
        if (scorePanel != null)
            scorePanel.SetActive(false);
        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = 0f;
            buttonCanvasGroup.interactable = false;
            buttonCanvasGroup.blocksRaycasts = false;
        }

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
            STSSceneLoader.Instance?.EndLoading();
            STSSceneLoader.Instance.LoadScene("STS_Boot");
            return;
        }

        if (!await ContinueRunAfterRetreatAsync())
        {
            STSSceneLoader.Instance?.EndLoading();
            if (choicePanel != null)
                choicePanel.SetActive(true);
            if (scorePanel != null)
                scorePanel.SetActive(true);
            if (buttonCanvasGroup != null)
            {
                buttonCanvasGroup.alpha = 1f;
                buttonCanvasGroup.interactable = true;
                buttonCanvasGroup.blocksRaycasts = true;
            }
            leavingRetreat = false;
            return;
        }

        STSRunAuditSystem.RecordNodeExited(RunManager.Instance, RunManager.Instance.currentNode, RunManager.Instance.currentNode, "STS_Map", "retreat_continue");
        STSSceneLoader.Instance?.EndLoading();
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
        leavingRetreat = true;
        STSSceneLoader.Instance?.BeginLoading();

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
        STSSceneLoader.Instance?.EndLoading();
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
            if (ShouldUseServerTokenPreview())
            {
                tokenRewardText.text = "Récompenses en tokens: calcul serveur...";
            }
            else
            {
                tokenRewardText.text = $"Récompenses en tokens: +{result.tokenReward}";
            }
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

        long requestedTokens = retreatPreviewResponse != null && retreatPreviewResponse.accepted
            ? Math.Max(0, retreatPreviewResponse.tokensPreview)
            : Mathf.Max(0, scoreResult.tokenReward);

        if (requestedTokens <= 0)
            return;

        try
        {
            bool hadBeforeBalance = TryReadTokenBalance(out long beforeBalance);
            bool granted = await TryGrantTokensWithReflectionAsync(requestedTokens);
            if (!granted)
            {
                Debug.LogWarning($"Retreat token reward could not be applied. No compatible method found on {nameof(PlayerProfileStore)}.");
                return;
            }

            long effectiveGranted = requestedTokens;
            if (hadBeforeBalance && TryReadTokenBalance(out long afterBalance))
            {
                effectiveGranted = Math.Max(0, afterBalance - beforeBalance);
            }

            if (tokenRewardText != null)
            {
                tokenRewardText.text = $"Tokens accordés: +{effectiveGranted}";
            }

            if (effectiveGranted != requestedTokens)
            {
                Debug.LogWarning($"Retreat token grant amount differed from requested preview. requested={requestedTokens} effective={effectiveGranted}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while granting retreat token reward: {ex}");
        }
    }

    private bool ShouldUseServerTokenPreview()
    {
        return RunManager.Instance != null
            && !RunManager.Instance.completedFinalAct
            && !RunManager.Instance.unrestrictedMode
            && !string.IsNullOrWhiteSpace(RunManager.Instance.runId);
    }

    private async Task<bool> ContinueRunAfterRetreatAsync()
    {
        if (RunManager.Instance == null)
            return false;

        if (string.IsNullOrWhiteSpace(RunManager.Instance.runId) || RunManager.Instance.unrestrictedMode)
        {
            RunManager.Instance?.ActAndRegenerateLocally();
            return true;
        }

        try
        {
            STSApiRunState nextState = await STSApiClient.RetreatContinueAsync(RunManager.Instance.runId);
            if (nextState == null)
            {
                Debug.LogWarning("Retreat continue request returned no state. Attempting to recover authoritative current run state.");
                STSApiCurrentRunResponse currentRun = await STSApiClient.CurrentRunAsync();
                if (currentRun != null && currentRun.hasRun && currentRun.run != null)
                {
                    STSApiRunState recoveredState = STSApiClient.ConvertToRunState(currentRun.run);
                    if (recoveredState != null && recoveredState.runId == RunManager.Instance.runId && recoveredState.act >= RunManager.Instance.act)
                    {
                        RunManager.Instance.ApplyRemoteRunState(recoveredState);
                        return true;
                    }
                }

                Debug.LogWarning("Retreat continue recovery did not produce a usable authoritative run state. Staying on retreat screen to avoid desync.");
                RunManager.Instance.EnableUnrestrictedMode("retreat continue unavailable, applying local continue fallback");
                RunManager.Instance.ActAndRegenerateLocally();
                return true;
            }

            RunManager.Instance.ApplyRemoteRunState(nextState);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to continue run through API after retreat: {ex.Message}. Attempting authoritative current-run recovery.");
            try
            {
                STSApiCurrentRunResponse currentRun = await STSApiClient.CurrentRunAsync();
                if (currentRun != null && currentRun.hasRun && currentRun.run != null)
                {
                    STSApiRunState recoveredState = STSApiClient.ConvertToRunState(currentRun.run);
                    if (recoveredState != null && recoveredState.runId == RunManager.Instance.runId && recoveredState.act >= RunManager.Instance.act)
                    {
                        RunManager.Instance.ApplyRemoteRunState(recoveredState);
                        return true;
                    }
                }
            }
            catch (Exception recoveryEx)
            {
                Debug.LogWarning($"Failed to recover current run after retreat continue failure: {recoveryEx.Message}");
            }

            RunManager.Instance.EnableUnrestrictedMode($"retreat continue failed: {ex.Message}");
            RunManager.Instance.ActAndRegenerateLocally();
            return true;
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

            EnsureLocalTokenFallbackIfPreviewMissing();
            return;
        }

        previewRequestStarted = true;
        previewRequestTask = LoadRetreatPreviewAsync();
        await previewRequestTask;
        EnsureLocalTokenFallbackIfPreviewMissing();
    }

    private void EnsureLocalTokenFallbackIfPreviewMissing()
    {
        if (retreatPreviewResponse != null && retreatPreviewResponse.accepted)
        {
            return;
        }

        if (tokenRewardText != null && scoreResult != null)
        {
            tokenRewardText.text = $"Récompenses en tokens: +{Mathf.Max(0, scoreResult.tokenReward)}";
        }
    }

    private async Task LoadRetreatPreviewAsync()
    {
        try
        {
            retreatPreviewResponse = await STSApiClient.RetreatPreviewAsync(RunManager.Instance.runId);
            if (leavingRetreat)
            {
                return;
            }
            if (retreatPreviewResponse == null || !retreatPreviewResponse.accepted)
            {
                Debug.LogWarning("Retreat preview request did not return an accepted response. Keeping local retreat summary.");
                if (tokenRewardText != null && scoreResult != null)
                {
                    tokenRewardText.text = $"Récompenses en tokens: +{Mathf.Max(0, scoreResult.tokenReward)}";
                }
                return;
            }

            RunManager.Instance.apiStatus = retreatPreviewResponse.status;
            RenderServerPreviewSummary(retreatPreviewResponse);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load retreat preview through API, keeping local summary: {ex.Message}");
            if (tokenRewardText != null && scoreResult != null)
            {
                tokenRewardText.text = $"Récompenses en tokens: +{Mathf.Max(0, scoreResult.tokenReward)}";
            }
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
            if (leavingRetreat)
            {
                return;
            }
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
        if (response == null || leavingRetreat)
            return;

        if (scoreTitleText != null)
        {
            scoreTitleText.text = "Dernier acte terminé";
        }

        if (scoreDetailsText != null)
        {
            scoreDetailsText.text = BuildServerBreakdownText(
                "Vous avez passé le dernier acte. Revenez plus tard pour pouvoir aller plus loin!",
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
            tokenRewardText.text = $"Tokens accordés: +{response.tokensGranted} (solde: {response.tokenBalance})";
        }
    }

    private void RenderServerPreviewSummary(STSApiRetreatPreviewResponse response)
    {
        if (response == null || leavingRetreat)
            return;

        if (scoreTitleText != null)
        {
            scoreTitleText.text = "Score de la partie";
        }

        if (scoreDetailsText != null)
        {
            scoreDetailsText.text = BuildServerBreakdownText(
                "Aperçu serveur pour cette fin d'acte",
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
            tokenRewardText.text = $"Tokens prévus: +{response.tokensPreview} (solde projete: {response.projectedTokenBalance})";
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
        sb.AppendLine($"Étages parcourus: +{visitedNodeScore}");
        sb.AppendLine($"Combats gagnés: +{combatVictoryScore}");
        sb.AppendLine($"Elites vaincues: +{eliteVictoryScore}");
        sb.AppendLine($"Événements visités: +{eventVisitedScore}");
        sb.AppendLine($"Actes atteints: +{actReachedScore}");
        sb.AppendLine($"Reliques possédées: +{relicOwnedScore}");
        sb.AppendLine($"Taille du deck: +{deckCardScore}");
        sb.AppendLine($"PV restants: +{remainingHpPercentScore}");
        sb.AppendLine();
        sb.AppendLine($"Score serveur: {score}");

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

    private bool TryReadTokenBalance(out long balance)
    {
        balance = 0;
        Type storeType = typeof(PlayerProfileStore);

        foreach (string methodName in TokenBalanceMethodCandidates)
        {
            MethodInfo method = storeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null || method.GetParameters().Length != 0)
                continue;

            object result = method.Invoke(null, null);
            if (TryConvertToLong(result, out balance))
                return true;
        }

        foreach (string memberName in TokenBalanceMemberCandidates)
        {
            PropertyInfo property = storeType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
            if (property != null)
            {
                object value = property.GetValue(null);
                if (TryConvertToLong(value, out balance))
                    return true;
            }

            FieldInfo field = storeType.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                object value = field.GetValue(null);
                if (TryConvertToLong(value, out balance))
                    return true;
            }
        }

        return false;
    }

    private static bool TryConvertToLong(object value, out long converted)
    {
        converted = 0;
        if (value == null)
            return false;

        switch (value)
        {
            case byte byteValue:
                converted = byteValue;
                return true;
            case sbyte sbyteValue:
                converted = sbyteValue;
                return true;
            case short shortValue:
                converted = shortValue;
                return true;
            case ushort ushortValue:
                converted = ushortValue;
                return true;
            case int intValue:
                converted = intValue;
                return true;
            case uint uintValue:
                converted = uintValue;
                return true;
            case long longValue:
                converted = longValue;
                return true;
            case ulong ulongValue:
                converted = ulongValue > long.MaxValue ? long.MaxValue : (long)ulongValue;
                return true;
            case float floatValue:
                converted = (long)floatValue;
                return true;
            case double doubleValue:
                converted = (long)doubleValue;
                return true;
            case decimal decimalValue:
                converted = (long)decimalValue;
                return true;
            default:
                return false;
        }
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
