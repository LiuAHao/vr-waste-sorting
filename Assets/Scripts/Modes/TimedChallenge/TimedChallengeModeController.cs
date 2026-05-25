using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TimedChallengeModeController : MonoBehaviour
{
    private const float FeedbackDurationSeconds = 1.4f;

    [SerializeField] private TimedChallengeConfig config;
    [SerializeField] private TimedChallengeSpawner spawner;

    private WasteHudView _hud;
    private WasteResultView _resultView;
    private WasteAnalyticsTracker _analytics;
    private Action _restartAction;

    private readonly List<Player> _players = new List<Player>();
    private readonly List<GarbageItem> _hiddenSceneItems = new List<GarbageItem>();

    private float _timeLimitSeconds;
    private float _remainingSeconds;
    private float _feedbackRemainingSeconds;
    private int _score;
    private int _correctCount;
    private int _wrongCount;
    private bool _isSessionActive;
    private bool _isFinished;

    public bool IsSessionActive => _isSessionActive && !_isFinished;
    public TimedChallengeConfig Config => config;

    public void Configure(TimedChallengeConfig challengeConfig, TimedChallengeSpawner challengeSpawner)
    {
        if (challengeConfig != null)
        {
            config = challengeConfig;
        }

        if (challengeSpawner != null)
        {
            spawner = challengeSpawner;
        }
    }

    public void Configure(
        WasteHudView hud,
        WasteResultView resultView,
        WasteAnalyticsTracker analytics,
        Action restartAction)
    {
        _hud = hud;
        _resultView = resultView;
        _analytics = analytics;
        _restartAction = restartAction;
    }

    public void StartChallenge()
    {
        if (_isSessionActive || _isFinished || config == null || spawner == null)
        {
            if (config == null)
            {
                Debug.LogWarning("TimedChallengeModeController: 未配置 TimedChallengeConfig。");
            }

            if (spawner == null)
            {
                Debug.LogWarning("TimedChallengeModeController: 未配置 TimedChallengeSpawner。");
            }

            return;
        }

        CachePlayers();
        HideSceneGarbage();
        ResetSessionState();

        _analytics.BeginSession();
        spawner.Initialize(config);

        _isSessionActive = true;
        _resultView.Hide();
        _hud.SetVisible(true);
        _hud.HideFeedback();
        SetPlayerInputEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        RefreshHud();
    }

    public void Tick(float deltaTime)
    {
        if (!_isSessionActive || _isFinished)
        {
            return;
        }

        _remainingSeconds -= deltaTime;
        if (_feedbackRemainingSeconds > 0f)
        {
            _feedbackRemainingSeconds -= deltaTime;
            if (_feedbackRemainingSeconds <= 0f)
            {
                _hud.HideFeedback();
            }
        }

        if (_remainingSeconds <= 0f)
        {
            _remainingSeconds = 0f;
            EndSession();
            return;
        }

        RefreshHud();
    }

    public void HandleClassification(ClassificationResult result)
    {
        if (!_isSessionActive || _isFinished || result == null)
        {
            return;
        }

        float elapsed = _timeLimitSeconds - _remainingSeconds;
        _analytics.RecordClassification(result, elapsed);

        if (result.IsCorrect)
        {
            _correctCount++;
            _score += config.ScorePerCorrect;
            ShowFeedback(true, "投放正确", BuildCorrectDetail(result));
        }
        else
        {
            _wrongCount++;
            _score -= config.PenaltyPerWrong;
            ShowFeedback(false, "投放错误", BuildWrongDetail(result));
            if (result.Item != null)
            {
                result.Item.MarkCompleted();
            }
        }

        if (result.Item != null && spawner != null)
        {
            spawner.HandleItemProcessed(result.Item);
        }

        RefreshHud();
    }

    private void EndSession()
    {
        if (_isFinished)
        {
            return;
        }

        _isFinished = true;
        _isSessionActive = false;
        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _hud.HideFeedback();

        TimedChallengeSessionStats stats = TimedChallengeSessionStats.FromRecords(_analytics.Records);
        WasteSessionSummary summary = _analytics.BuildSummary(
            stats.TotalProcessedCount,
            _correctCount,
            _wrongCount,
            _score,
            _timeLimitSeconds - _remainingSeconds,
            _timeLimitSeconds,
            modeName: "限时挑战",
            mostMistakenItemName: stats.MostMistakenItemName,
            mostMistakenItemCount: stats.MostMistakenItemCount,
            totalProcessedCount: stats.TotalProcessedCount,
            mistakeSummaryText: stats.BuildMistakeSummaryText());

        _resultView.Show(summary, _restartAction);
    }

    private void ResetSessionState()
    {
        _timeLimitSeconds = config.TimeLimitSeconds;
        _remainingSeconds = _timeLimitSeconds;
        _feedbackRemainingSeconds = 0f;
        _score = 0;
        _correctCount = 0;
        _wrongCount = 0;
        _isFinished = false;
    }

    private void HideSceneGarbage()
    {
        _hiddenSceneItems.Clear();
        GarbageItem[] sceneItems = FindObjectsOfType<GarbageItem>(true);
        for (int i = 0; i < sceneItems.Length; i++)
        {
            GarbageItem item = sceneItems[i];
            if (item == null || !item.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (spawner != null && IsSpawnerManagedItem(item))
            {
                continue;
            }

            item.gameObject.SetActive(false);
            _hiddenSceneItems.Add(item);
        }
    }

    private void CachePlayers()
    {
        _players.Clear();
        _players.AddRange(FindObjectsOfType<Player>());
    }

    private void ShowFeedback(bool isCorrect, string title, string detail)
    {
        _hud.ShowFeedback(isCorrect, title, detail);
        _feedbackRemainingSeconds = FeedbackDurationSeconds;
    }

    private void RefreshHud()
    {
        int processedCount = _correctCount + _wrongCount;
        _hud.SetTimedChallengeStats(_remainingSeconds, _score, processedCount);
    }

    private void SetPlayerInputEnabled(bool enabled)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            Player player = _players[i];
            if (player != null)
            {
                player.SetInputEnabled(enabled);
            }
        }
    }

    private bool IsSpawnerManagedItem(GarbageItem item)
    {
        if (spawner == null || item == null)
        {
            return false;
        }

        IReadOnlyList<GarbageItem> activeItems = spawner.ActiveItems;
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i] == item)
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildCorrectDetail(ClassificationResult result)
    {
        string itemName = result.Item != null ? result.Item.ItemName : "物品";
        string binName = result.Bin != null ? result.Bin.DisplayName : "目标桶";
        return itemName + " 已正确投入 " + binName;
    }

    private static string BuildWrongDetail(ClassificationResult result)
    {
        string itemName = result.Item != null ? result.Item.ItemName : "物品";
        string correctCategory = WasteCategoryText.Format(result.CorrectCategory);
        string reason = string.IsNullOrWhiteSpace(result.Reason) ? "分类不匹配" : result.Reason;
        return itemName + " 应投放到 " + correctCategory + "。原因：" + reason;
    }
}

public sealed class TimedChallengeSessionStats
{
    public int TotalProcessedCount { get; private set; }
    public string MostMistakenItemName { get; private set; }
    public int MostMistakenItemCount { get; private set; }
    public Dictionary<string, int> MistakeCountByCategoryPair { get; } = new Dictionary<string, int>();

    public static TimedChallengeSessionStats FromRecords(IReadOnlyList<ClassificationRecord> records)
    {
        TimedChallengeSessionStats stats = new TimedChallengeSessionStats();
        if (records == null || records.Count <= 0)
        {
            return stats;
        }

        stats.TotalProcessedCount = records.Count;

        Dictionary<string, int> mistakeCountByItem = new Dictionary<string, int>();
        for (int i = 0; i < records.Count; i++)
        {
            ClassificationRecord record = records[i];
            if (record.IsCorrect)
            {
                continue;
            }

            string itemName = string.IsNullOrWhiteSpace(record.ItemName) ? "未知物品" : record.ItemName;
            if (!mistakeCountByItem.ContainsKey(itemName))
            {
                mistakeCountByItem[itemName] = 0;
            }

            mistakeCountByItem[itemName]++;

            string pairKey = WasteCategoryText.Format(record.CorrectCategory) + " -> " + WasteCategoryText.Format(record.SelectedCategory);
            if (!stats.MistakeCountByCategoryPair.ContainsKey(pairKey))
            {
                stats.MistakeCountByCategoryPair[pairKey] = 0;
            }

            stats.MistakeCountByCategoryPair[pairKey]++;
        }

        int highestCount = 0;
        foreach (KeyValuePair<string, int> pair in mistakeCountByItem)
        {
            if (pair.Value > highestCount)
            {
                highestCount = pair.Value;
                stats.MostMistakenItemName = pair.Key;
                stats.MostMistakenItemCount = pair.Value;
            }
        }

        return stats;
    }

    public string BuildMistakeSummaryText()
    {
        if (TotalProcessedCount <= 0)
        {
            return "本轮没有完成任何分类。";
        }

        int correctCount = TotalProcessedCount - CountMistakes();
        if (string.IsNullOrWhiteSpace(MostMistakenItemName))
        {
            return "你在限定时间内完成了 " + TotalProcessedCount + " 次分类，其中 " + correctCount + " 次正确。";
        }

        return "你在限定时间内完成了 " + TotalProcessedCount + " 次分类，其中 " + correctCount + " 次正确。最容易混淆的是 "
            + MostMistakenItemName + "，建议下一轮重点练习该类物品判断。";
    }

    private int CountMistakes()
    {
        int mistakeCount = 0;
        foreach (KeyValuePair<string, int> pair in MistakeCountByCategoryPair)
        {
            mistakeCount += pair.Value;
        }

        return mistakeCount;
    }
}
