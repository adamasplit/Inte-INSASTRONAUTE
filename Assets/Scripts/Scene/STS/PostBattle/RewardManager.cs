using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
public class RewardManager : MonoBehaviour, IRewardFlowHost
{
    public Transform rewardList;

    public GameObject cardRewardPrefab;
    public GameObject relicRewardPrefab;
    public GameObject goldRewardPrefab;
    public GameObject baseRelicUpgradeRewardPrefab;

    public GameObject continueButton;

    List<RewardEntryView> activeEntries = new();
    bool goingToMap = true;
    void Start()
    {
        Reward reward = null;
        if (RunManager.Instance != null
            && !RunManager.Instance.backendRewardClaimUnavailable
            && TryBuildRewardFromServerPending(out Reward serverReward))
        {
            reward = serverReward;
            goingToMap = !RunManager.Instance.bossEncounter;
            RunManager.Instance.pendingReward = null;
        }
        else if (RunManager.Instance!=null &&RunManager.Instance.pendingReward != null)
        {
            reward = RunManager.Instance.pendingReward;
            goingToMap = !RunManager.Instance.bossEncounter;
        }
        else
        {
            
            CombatResult result = new CombatResult
            {
                floor = 1,
                elite = true,
                boss = true,
                act = 1
            };
            reward = RewardGenerator.GenerateReward(result);
        }

        if (reward == null)
        {
            reward = new Reward();
        }

        NormalizeCardRewardEntries(reward);
        
        foreach (var item in reward.items)
        {
            SpawnReward(item);
        }

        if (activeEntries.Count == 0 && continueButton != null)
        {
            continueButton.SetActive(true);
        }

        STSSceneLoader.Instance?.SceneReady();
    }

    void NormalizeCardRewardEntries(Reward reward)
    {
        if (reward == null)
            return;

        List<CardReward> cardRewards = new List<CardReward>();
        foreach (RewardItem item in reward.items)
        {
            if (item is CardReward cardReward)
            {
                cardRewards.Add(cardReward);
            }
        }

        if (cardRewards.Count == 0)
            return;

        string mergedServerRewardId = null;
        for (int i = 0; i < cardRewards.Count; i++)
        {
            if (cardRewards[i] != null && !string.IsNullOrWhiteSpace(cardRewards[i].serverRewardId))
            {
                mergedServerRewardId = cardRewards[i].serverRewardId;
                break;
            }
        }

        bool serverAuthoredCardReward = !string.IsNullOrWhiteSpace(mergedServerRewardId);

        List<CardInstance> merged = new List<CardInstance>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (CardReward cardReward in cardRewards)
        {
            if (cardReward?.choices == null)
                continue;

            foreach (CardInstance card in cardReward.choices)
            {
                if (card?.data == null)
                    continue;

                string cardId = card.data.id;
                if (string.IsNullOrWhiteSpace(cardId) || seen.Contains(cardId))
                    continue;

                seen.Add(cardId);
                merged.Add(card);
                if (merged.Count >= 3)
                    break;
            }

            if (merged.Count >= 3)
                break;
        }

        if (merged.Count < 3 && !serverAuthoredCardReward)
        {
            CombatResult fallbackResult = new CombatResult
            {
                floor = RunManager.Instance != null ? RunManager.Instance.currentFloor : 1,
                elite = RunManager.Instance != null && RunManager.Instance.eliteEncounter,
                boss = RunManager.Instance != null && RunManager.Instance.bossEncounter,
                act = RunManager.Instance != null ? RunManager.Instance.act : 0
            };

            CardReward fallbackCardReward = RewardGenerator.GenerateCardReward(fallbackResult);
            if (fallbackCardReward?.choices != null)
            {
                foreach (CardInstance card in fallbackCardReward.choices)
                {
                    if (card?.data == null)
                        continue;

                    string cardId = card.data.id;
                    if (string.IsNullOrWhiteSpace(cardId) || seen.Contains(cardId))
                        continue;

                    seen.Add(cardId);
                    merged.Add(card);
                    if (merged.Count >= 3)
                        break;
                }
            }
        }

        if (merged.Count > 3)
        {
            merged = merged.GetRange(0, 3);
        }

        reward.items.RemoveAll(item => item is CardReward);
        reward.items.Add(new CardReward
        {
            choices = merged,
            serverRewardId = mergedServerRewardId
        });
    }

    bool TryBuildRewardFromServerPending(out Reward reward)
    {
        reward = null;
        if (RunManager.Instance == null)
            return false;

        List<JToken> pending = RunManager.Instance.ConsumeServerPendingRewards();
        if (pending == null || pending.Count == 0)
            return false;

        Reward parsed = new Reward();
        foreach (JToken token in pending)
        {
            TryAppendPendingRewardToken(token, parsed);
        }

        if (parsed.items.Count == 0)
        {
            Debug.LogWarning($"[STS-RUN] Received {pending.Count} pending rewards from backend but none could be converted. Falling back to local reward generation.");
            return false;
        }

        reward = parsed;
        Debug.Log($"[STS-RUN] Built reward screen from {parsed.items.Count} backend pending rewards.");
        return true;
    }

    void TryAppendPendingRewardToken(JToken token, Reward reward)
    {
        if (token == null || reward == null)
            return;

        if (token.Type == JTokenType.Array)
        {
            foreach (JToken entry in token)
            {
                TryAppendPendingRewardToken(entry, reward);
            }
            return;
        }

        if (token.Type != JTokenType.Object)
            return;

        JObject obj = (JObject)token;
        string rewardId = ReadString(obj, "rewardId", "id");
        string type = ReadString(obj, "type", "rewardType", "kind", "category");
        if (string.IsNullOrWhiteSpace(type))
        {
            if (HasAny(obj, "choices", "cards", "cardIds", "cardId", "card"))
                type = "card";
            else if (HasAny(obj, "relicId", "relic"))
                type = ReadInt(obj, "stage", "upgradeStage") != null ? "relic_upgrade" : "relic";
            else if (HasAny(obj, "amount", "gold", "value"))
                type = "gold";
        }

        string normalized = (type ?? string.Empty).Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "gold":
            case "gold_reward":
            case "currency":
                int amount = Mathf.Max(0, ReadInt(obj, "amount", "gold", "value") ?? 0);
                if (amount > 0)
                {
                    reward.items.Add(new GoldReward { amount = amount, serverRewardId = rewardId });
                }
                return;

            case "relic":
            case "relic_reward":
                {
                    string relicId = ReadString(obj, "relicId", "relic", "id");
                    Relic relic = STSApiClient.CreateRelicFromId(relicId);
                    if (relic == null)
                        return;

                    int? stage = ReadInt(obj, "stage", "relicStage");
                    if (stage != null && relic is BaseRelic baseRelic)
                    {
                        baseRelic.stage = Mathf.Max(0, stage.Value);
                    }

                    reward.items.Add(new RelicReward { relic = relic, serverRewardId = rewardId });
                    return;
                }

            case "relic_upgrade":
            case "base_relic_upgrade":
            case "base_relic_upgrade_reward":
                {
                    string relicId = ReadString(obj, "relicId", "relic", "id");
                    Relic relic = STSApiClient.CreateRelicFromId(relicId);
                    if (!(relic is BaseRelic baseRelic))
                        return;

                    int stage = Mathf.Max(0, ReadInt(obj, "stage", "upgradeStage") ?? baseRelic.stage + 1);
                    reward.items.Add(new BaseRelicUpgradeReward
                    {
                        relic = baseRelic,
                        stage = stage,
                        serverRewardId = rewardId
                    });
                    return;
                }

            default:
                {
                    List<CardInstance> choices = ParseCardChoices(obj);
                    if (choices.Count > 0)
                    {
                        reward.items.Add(new CardReward { choices = choices, serverRewardId = rewardId });
                    }
                    return;
                }
        }
    }

    public async Task<bool> TryClaimServerRewardAsync(RewardItem rewardItem, string selectedCardId = null)
    {
        if (rewardItem == null || string.IsNullOrWhiteSpace(rewardItem.serverRewardId))
        {
            return true;
        }

        if (RunManager.Instance == null || string.IsNullOrWhiteSpace(RunManager.Instance.runId) || RunManager.Instance.unrestrictedMode)
        {
            return true;
        }

        try
        {
            STSApiClaimRewardResponse response = await STSApiClient.ClaimRewardAsync(
                RunManager.Instance.runId,
                rewardItem.serverRewardId,
                selectedCardId
            );

            if (response == null || !response.accepted)
            {
                Debug.LogWarning($"[STS-RUN] Reward claim rejected for rewardId={rewardItem.serverRewardId}.");
                return false;
            }

            RunManager.Instance.serverRunInventoryPatch = response.runInventory;
            RunManager.Instance.serverPendingRewards = response.pendingRewards != null
                ? new List<JToken>(response.pendingRewards)
                : new List<JToken>();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[STS-RUN] Reward claim failed for rewardId={rewardItem.serverRewardId}: {ex.Message}");
            return false;
        }
    }

    List<CardInstance> ParseCardChoices(JObject obj)
    {
        List<CardInstance> cards = new List<CardInstance>();
        HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AppendCardsFromToken(obj["choices"], cards, seenIds);
        AppendCardsFromToken(obj["cards"], cards, seenIds);
        AppendCardsFromToken(obj["cardChoices"], cards, seenIds);
        AppendCardsFromToken(obj["card_choices"], cards, seenIds);
        AppendCardsFromToken(obj["cardIds"], cards, seenIds);
        AppendCardsFromToken(obj["card_ids"], cards, seenIds);
        AppendCardsFromToken(obj["options"], cards, seenIds);

        if (cards.Count == 0)
        {
            AddCardById(ReadString(obj, "cardId", "card", "id"), cards, seenIds);
        }

        return cards;
    }

    void AppendCardsFromToken(JToken token, List<CardInstance> cards, HashSet<string> seenIds)
    {
        if (token == null)
            return;

        if (token.Type == JTokenType.Array)
        {
            foreach (JToken entry in token)
            {
                AppendCardsFromToken(entry, cards, seenIds);
            }
            return;
        }

        if (token.Type == JTokenType.Object)
        {
            JObject obj = (JObject)token;
            AddCardFromTokenObject(obj, cards, seenIds);
            return;
        }

        AddCardById(token.ToString(), cards, seenIds);
    }

    void AddCardFromTokenObject(JObject obj, List<CardInstance> cards, HashSet<string> seenIds)
    {
        if (obj == null)
            return;

        string cardId = ReadString(obj, "cardId", "id", "card", "name");
        if (string.IsNullOrWhiteSpace(cardId) || seenIds.Contains(cardId))
            return;

        STSCardData cardData = FindCardDataById(cardId);
        if (cardData == null)
            return;

        if (cardData.favoredCharacter == SelectableCharacter.Starting || cardData.favoredCharacter == SelectableCharacter.Impossible)
            return;

        CardInstance card = new CardInstance(cardData);
        ApplyServerEnchantments(card, obj["enchantments"]);

        seenIds.Add(cardId);
        cards.Add(card);
    }

    void ApplyServerEnchantments(CardInstance card, JToken enchantmentsToken)
    {
        if (card == null || enchantmentsToken == null || enchantmentsToken.Type == JTokenType.Null)
            return;

        List<JToken> entries = new List<JToken>();
        if (enchantmentsToken.Type == JTokenType.Array)
        {
            entries.AddRange(enchantmentsToken.Children());
        }
        else
        {
            entries.Add(enchantmentsToken);
        }

        bool anyApplied = false;
        int fallbackCharges = 0;

        foreach (JToken entry in entries)
        {
            if (entry == null || entry.Type != JTokenType.Object)
                continue;

            JObject enchantObj = (JObject)entry;
            string enchantName = ReadString(enchantObj, "enchantmentClass", "enchantmentId", "id", "name");
            int level = Mathf.Max(1, ReadInt(enchantObj, "level", "charges", "value") ?? 1);

            if (TryApplyExplicitEnchantment(card, enchantName, level))
            {
                anyApplied = true;
            }
            else
            {
                fallbackCharges += level;
            }
        }

        if (!anyApplied && fallbackCharges > 0)
        {
            // Backend may send placeholder identifiers (e.g. EliteEnchantment).
            // In that case, ensure elite rewards still get at least one real enchant.
            EnchantManager.ApplyEnchant(card, fallbackCharges, includeTreasureEnchants: true);
        }
    }

    bool TryApplyExplicitEnchantment(CardInstance card, string enchantName, int level)
    {
        if (card == null || string.IsNullOrWhiteSpace(enchantName))
            return false;

        string trimmed = enchantName.Trim();

        if (trimmed.EndsWith("Enchantment", StringComparison.OrdinalIgnoreCase))
        {
            string shortName = trimmed.Substring(0, trimmed.Length - "Enchantment".Length);
            if (Enum.TryParse(shortName, true, out EnchantManager.EnchantType enumType))
            {
                CardEnchantment enchantment = EnchantManager.GetEnchantByType(enumType, level);
                if (enchantment != null)
                {
                    card.AddEnchantment(enchantment);
                    return true;
                }
            }
        }

        Type enchantmentType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => string.Equals(t.Name, trimmed, StringComparison.Ordinal));

        if (enchantmentType == null || !typeof(EnchantmentData).IsAssignableFrom(enchantmentType))
            return false;

        try
        {
            EnchantmentData data = Activator.CreateInstance(enchantmentType) as EnchantmentData;
            if (data == null)
                return false;

            card.AddEnchantment(new CardEnchantment
            {
                data = data,
                level = level
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    void AddCardById(string cardId, List<CardInstance> cards, HashSet<string> seenIds)
    {
        if (string.IsNullOrWhiteSpace(cardId) || seenIds.Contains(cardId))
            return;

        STSCardData cardData = FindCardDataById(cardId);
        if (cardData == null)
            return;

        if (cardData.favoredCharacter == SelectableCharacter.Starting || cardData.favoredCharacter == SelectableCharacter.Impossible)
            return;

        seenIds.Add(cardId);
        cards.Add(new CardInstance(cardData));
    }

    STSCardData FindCardDataById(string cardId)
    {
        if (STSCardDatabase.allCards == null)
            return null;

        foreach (STSCardData card in STSCardDatabase.allCards)
        {
            if (card == null)
                continue;

            if (string.Equals(card.id, cardId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(card.cardName, cardId, StringComparison.OrdinalIgnoreCase))
            {
                return card;
            }
        }

        return null;
    }

    static string ReadString(JObject obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken token)
                && token != null
                && token.Type != JTokenType.Null)
            {
                string value = token.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return null;
    }

    static int? ReadInt(JObject obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken token)
                || token == null
                || token.Type == JTokenType.Null)
            {
                continue;
            }

            if (token.Type == JTokenType.Integer)
                return token.Value<int>();

            if (int.TryParse(token.ToString(), out int parsed))
                return parsed;
        }

        return null;
    }

    static bool HasAny(JObject obj, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken token)
                && token != null
                && token.Type != JTokenType.Null)
            {
                return true;
            }
        }

        return false;
    }

    void SpawnReward(RewardItem item)
    {
        GameObject prefab = null;

        if (item is CardReward)
        {
            prefab = cardRewardPrefab;
        }
        else if (item is RelicReward)
        {   
            prefab = relicRewardPrefab;
        }
        else if (item is GoldReward)
        {
            prefab = goldRewardPrefab;
        }
        else if (item is BaseRelicUpgradeReward)
        {
            prefab = baseRelicUpgradeRewardPrefab;
        }
        if (prefab == null)
            return;

        var obj = Instantiate(prefab, rewardList);

        var view = obj.GetComponent<RewardEntryView>();
        view.Init(item, this);

        UILayoutHelper.ApplyPreferredSizeAfterFrame(this, obj.transform as RectTransform, fitWidth: true, fitHeight: true, extraWidth: 20f, extraHeight: 12f);

        activeEntries.Add(view);
    }

    public void NotifyClaimed(RewardEntryView entry)
    {
        activeEntries.Remove(entry);

        if (activeEntries.Count == 0)
        {
            continueButton.SetActive(true);
        }
    }

    public void Continue()
    {
        RunManager.Instance.pendingReward = null;
        if (goingToMap)
        {
            STSSceneLoader.Instance.LoadScene("STS_Map");
        }
        else
        {
            STSSceneLoader.Instance.LoadScene("STS_Retreat");
        }
    }
}