using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageProgressionModeController : MonoBehaviour
{
    private const float FeedbackDurationSeconds = 1.4f;

    [SerializeField] private StageProgressionConfig config;
    [SerializeField] private StageGarbageSpawner spawner;

    private WasteHudView _hud;
    private WasteResultView _resultView;
    private StageTransitionView _transitionView;
    private WasteAnalyticsTracker _analytics;
    private Action _restartAction;

    private readonly List<Player> _players = new List<Player>();
    private readonly List<GarbageItem> _hiddenSceneItems = new List<GarbageItem>();
    private readonly List<StageRunRecord> _stageRecords = new List<StageRunRecord>();

    private int _currentStageIndex;
    private float _stageTimeLimitSeconds;
    private float _remainingSeconds;
    private float _feedbackRemainingSeconds;
    private float _transitionRemainingSeconds;
    private int _stageTargetCount;
    private int _stageSpawnedCount;
    private int _stageCorrectCount;
    private int _stageWrongCount;
    private int _selectedDifficultyStageIndex;
    private int _totalScore;
    private int _totalCorrectCount;
    private int _totalWrongCount;
    private float _totalElapsedSeconds;
    private bool _isSessionActive;
    private bool _isFinished;
    private bool _isInTransition;
    private int _failedStageIndex = -1;
    private bool _allStagesCleared;

    public bool IsSessionActive => _isSessionActive && !_isFinished;

    public void Configure(StageProgressionConfig progressionConfig, StageGarbageSpawner stageSpawner)
    {
        if (progressionConfig != null)
        {
            config = progressionConfig;
        }

        if (stageSpawner != null)
        {
            spawner = stageSpawner;
        }
    }

    public void Configure(
        WasteHudView hud,
        WasteResultView resultView,
        StageTransitionView transitionView,
        WasteAnalyticsTracker analytics,
        Action restartAction)
    {
        _hud = hud;
        _resultView = resultView;
        _transitionView = transitionView;
        _analytics = analytics;
        _restartAction = restartAction;
    }

    public void AbortSession()
    {
        _isSessionActive = false;
        _isFinished = false;
        _isInTransition = false;
        _transitionRemainingSeconds = 0f;
        _feedbackRemainingSeconds = 0f;

        if (spawner != null)
        {
            spawner.ClearSpawnedItems();
        }

        RestoreSceneGarbage();
        ResetRunState();

        if (_transitionView != null)
        {
            _transitionView.Hide();
        }

        if (_hud != null)
        {
            _hud.HideFeedback();
            _hud.SetVisible(false);
        }

        if (_resultView != null)
        {
            _resultView.Hide();
        }

        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public StageProgressionConfig Config => config;

    public int StageCount => config != null ? config.StageCount : 0;

    public void StartProgression(int stageIndex)
    {
        if (_isSessionActive || config == null || spawner == null)
        {
            if (config == null)
            {
                Debug.LogWarning("StageProgressionModeController: 未配置 StageProgressionConfig。");
            }

            if (spawner == null)
            {
                Debug.LogWarning("StageProgressionModeController: 未配置 StageGarbageSpawner。");
            }

            return;
        }

        if (stageIndex < 0 || stageIndex >= config.StageCount)
        {
            Debug.LogWarning("StageProgressionModeController: 无效的难度索引 " + stageIndex);
            return;
        }

        if (config.GetStage(stageIndex) == null)
        {
            Debug.LogWarning("StageProgressionModeController: 难度配置为空，index=" + stageIndex);
            return;
        }

        AbortSession();
        CachePlayers();
        HideSceneGarbage();
        ResetRunState();

        _analytics.BeginSession();
        _isSessionActive = true;
        _resultView?.Hide();
        _transitionView?.Hide();
        _hud?.SetVisible(true);
        _hud?.HideFeedback();
        SetPlayerInputEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _selectedDifficultyStageIndex = stageIndex;
        BeginStage(0);
    }

    public void Tick(float deltaTime)
    {
        if (!_isSessionActive || _isFinished)
        {
            return;
        }

        if (_isInTransition)
        {
            _transitionRemainingSeconds -= deltaTime;
            if (_transitionRemainingSeconds <= 0f)
            {
                _isInTransition = false;
                _transitionView?.Hide();
                if (_isSessionActive && !_isFinished)
                {
                    BeginStage(_currentStageIndex);
                }
            }

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
            FailAtCurrentStage();
            return;
        }

        RefreshHud();
    }

    public void HandleClassification(ClassificationResult result)
    {
        if (!_isSessionActive || _isFinished || _isInTransition || result == null)
        {
            return;
        }

        StageDefinition stage = config.GetStage(_currentStageIndex);
        if (stage == null)
        {
            return;
        }

        float stageElapsed = _stageTimeLimitSeconds - _remainingSeconds;
        _analytics.RecordClassification(result, _totalElapsedSeconds + stageElapsed, _currentStageIndex, stage.stageName);

        if (result.IsCorrect)
        {
            _stageCorrectCount++;
            _totalCorrectCount++;
            _totalScore += config.ScorePerCorrect;
            ShowFeedback(true, "投放正确", BuildCorrectDetail(result));
        }
        else
        {
            _stageWrongCount++;
            _totalWrongCount++;
            _totalScore -= config.PenaltyPerWrong;
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
        TryResolveStageProgress();
    }

    private void BeginStage(int stageIndex)
    {
        StageDefinition stage = config.GetStage(stageIndex);
        if (stage == null)
        {
            EndRunSuccess();
            return;
        }

        _currentStageIndex = stageIndex;
        _stageTimeLimitSeconds = stage.timeLimitSeconds;
        _remainingSeconds = _stageTimeLimitSeconds;
        _stageTargetCount = Mathf.Max(1, stage.targetCount);
        _stageCorrectCount = 0;
        _stageWrongCount = 0;
        _stageSpawnedCount = 0;
        _feedbackRemainingSeconds = 0f;
        _hud?.HideFeedback();

        int spawnedCount = spawner.SpawnStage(stage);
        _stageSpawnedCount = spawnedCount;
        SetPlayerInputEnabled(true);
        RefreshHud();

        if (spawnedCount < _stageTargetCount)
        {
            ShowFeedback(
                false,
                "垃圾生成不足",
                "本关应生成 " + _stageTargetCount + " 件，实际只有 " + spawnedCount
                + " 件。请环顾玩家附近，或检查关卡垃圾配置。");
        }
    }

    private void CompleteCurrentStage()
    {
        StageDefinition currentStage = config.GetStage(_currentStageIndex);
        float stageElapsed = _stageTimeLimitSeconds - _remainingSeconds;
        _totalElapsedSeconds += stageElapsed;

        _stageRecords.Add(new StageRunRecord(
            _currentStageIndex,
            currentStage != null ? currentStage.stageId : string.Empty,
            currentStage != null ? currentStage.stageName : string.Empty,
            _stageCorrectCount,
            _stageWrongCount,
            stageElapsed,
            true));

        int nextStageIndex = _currentStageIndex + 1;
        if (nextStageIndex <= _selectedDifficultyStageIndex && nextStageIndex < config.StageCount)
        {
            EnterStageTransition(currentStage, nextStageIndex, stageElapsed);
            return;
        }

        _allStagesCleared = true;
        EndRunSuccess();
    }

    private void TryResolveStageProgress()
    {
        if (_stageCorrectCount >= _stageTargetCount)
        {
            CompleteCurrentStage();
            return;
        }

        if (!IsStageGarbageExhausted())
        {
            return;
        }

        ShowFeedback(
            false,
            "本关未达标",
            "本关垃圾已全部处理，但正确分类仅 " + _stageCorrectCount + "/" + _stageTargetCount + " 件。");
        FailAtCurrentStage();
    }

    private bool IsStageGarbageExhausted()
    {
        int processedCount = _stageCorrectCount + _stageWrongCount;
        if (_stageSpawnedCount > 0 && processedCount >= _stageSpawnedCount)
        {
            return true;
        }

        return spawner != null && spawner.ActiveItems.Count <= 0 && processedCount > 0;
    }

    private void FailAtCurrentStage()
    {
        float stageElapsed = _stageTimeLimitSeconds - _remainingSeconds;
        _totalElapsedSeconds += stageElapsed;
        _failedStageIndex = _currentStageIndex;

        StageDefinition stage = config.GetStage(_currentStageIndex);
        _stageRecords.Add(new StageRunRecord(
            _currentStageIndex,
            stage != null ? stage.stageId : string.Empty,
            stage != null ? stage.stageName : string.Empty,
            _stageCorrectCount,
            _stageWrongCount,
            stageElapsed,
            false));

        EndRunFailed();
    }

    private void EndRunSuccess()
    {
        FinishSession();
        StageProgressionSessionStats stats = StageProgressionSessionStats.FromRecords(_analytics.Records, _stageRecords);
        StageDefinition selectedDifficultyStage = config.GetStage(_selectedDifficultyStageIndex);
        string difficultyName = selectedDifficultyStage != null ? selectedDifficultyStage.stageName : string.Empty;
        WasteSessionSummary summary = _analytics.BuildSummary(
            SumConfiguredStageTargets(_selectedDifficultyStageIndex),
            _totalCorrectCount,
            _totalWrongCount,
            _totalScore,
            _totalElapsedSeconds,
            SumConfiguredStageTime(_selectedDifficultyStageIndex),
            isStageProgression: true,
            modeName: config.ModeDisplayName,
            mostMistakenItemName: stats.MostMistakenItemName,
            mostMistakenItemCount: stats.MostMistakenItemCount,
            mistakeSummaryText: stats.BuildSummaryText(_allStagesCleared, difficultyName, CountClearedStages(), _failedStageIndex),
            clearedStageCount: CountClearedStages(),
            failedStageIndex: _failedStageIndex,
            allStagesCleared: _allStagesCleared,
            selectedDifficultyName: difficultyName);

        _resultView.Show(summary, ResolveRestartAction());
    }

    private void EndRunFailed()
    {
        FinishSession();
        StageProgressionSessionStats stats = StageProgressionSessionStats.FromRecords(_analytics.Records, _stageRecords);
        StageDefinition selectedDifficultyStage = config.GetStage(_selectedDifficultyStageIndex);
        string difficultyName = selectedDifficultyStage != null ? selectedDifficultyStage.stageName : string.Empty;
        WasteSessionSummary summary = _analytics.BuildSummary(
            SumConfiguredStageTargets(_selectedDifficultyStageIndex),
            _totalCorrectCount,
            _totalWrongCount,
            _totalScore,
            _totalElapsedSeconds,
            SumConfiguredStageTime(_selectedDifficultyStageIndex),
            isStageProgression: true,
            modeName: config.ModeDisplayName,
            mostMistakenItemName: stats.MostMistakenItemName,
            mostMistakenItemCount: stats.MostMistakenItemCount,
            mistakeSummaryText: stats.BuildSummaryText(false, difficultyName, CountClearedStages(), _failedStageIndex),
            clearedStageCount: CountClearedStages(),
            failedStageIndex: _failedStageIndex,
            allStagesCleared: false,
            selectedDifficultyName: difficultyName);

        _resultView.Show(summary, ResolveRestartAction());
    }

    private Action ResolveRestartAction()
    {
        if (_restartAction != null)
        {
            return _restartAction;
        }

        return WasteGameBootstrap.Instance != null
            ? (Action)WasteGameBootstrap.Instance.ReturnToStartMenu
            : null;
    }

    private void FinishSession()
    {
        if (_isFinished)
        {
            return;
        }

        _isFinished = true;
        _isSessionActive = false;
        _isInTransition = false;
        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _hud?.HideFeedback();
        _transitionView?.Hide();
        spawner?.ClearSpawnedItems();
        RestoreSceneGarbage();
    }

    private void OnDestroy()
    {
        RestoreSceneGarbage();
    }

    private void ResetRunState()
    {
        _currentStageIndex = 0;
        _selectedDifficultyStageIndex = 0;
        _totalScore = 0;
        _totalCorrectCount = 0;
        _totalWrongCount = 0;
        _totalElapsedSeconds = 0f;
        _failedStageIndex = -1;
        _allStagesCleared = false;
        _isFinished = false;
        _isInTransition = false;
        _stageSpawnedCount = 0;
        _stageRecords.Clear();
    }

    private int CountClearedStages()
    {
        int count = 0;
        for (int i = 0; i < _stageRecords.Count; i++)
        {
            if (_stageRecords[i].IsCleared)
            {
                count++;
            }
        }

        return count;
    }

    private float SumConfiguredStageTime(int lastStageIndex)
    {
        float total = 0f;
        int resolvedLastStageIndex = Mathf.Clamp(lastStageIndex, 0, Mathf.Max(0, config.StageCount - 1));
        for (int i = 0; i <= resolvedLastStageIndex; i++)
        {
            StageDefinition stage = config.GetStage(i);
            if (stage != null)
            {
                total += stage.timeLimitSeconds;
            }
        }

        return total;
    }

    private int SumConfiguredStageTargets(int lastStageIndex)
    {
        int total = 0;
        int resolvedLastStageIndex = Mathf.Clamp(lastStageIndex, 0, Mathf.Max(0, config.StageCount - 1));
        for (int i = 0; i <= resolvedLastStageIndex; i++)
        {
            StageDefinition stage = config.GetStage(i);
            if (stage != null)
            {
                total += Mathf.Max(1, stage.targetCount);
            }
        }

        return total;
    }

    private void HideSceneGarbage()
    {
        RestoreSceneGarbage();

        GarbageItem[] sceneItems = FindObjectsOfType<GarbageItem>(true);
        for (int i = 0; i < sceneItems.Length; i++)
        {
            GarbageItem item = sceneItems[i];
            if (item == null || !item.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (IsSpawnerManagedItem(item))
            {
                continue;
            }

            item.gameObject.SetActive(false);
            _hiddenSceneItems.Add(item);
        }
    }

    private void RestoreSceneGarbage()
    {
        for (int i = _hiddenSceneItems.Count - 1; i >= 0; i--)
        {
            GarbageItem item = _hiddenSceneItems[i];
            if (item != null)
            {
                item.gameObject.SetActive(true);
            }
        }

        _hiddenSceneItems.Clear();
    }

    private void CachePlayers()
    {
        _players.Clear();
        _players.AddRange(FindObjectsOfType<Player>());
    }

    private void ShowFeedback(bool isCorrect, string title, string detail)
    {
        if (_hud == null)
        {
            return;
        }

        _hud.ShowFeedback(isCorrect, title, detail);
        _feedbackRemainingSeconds = FeedbackDurationSeconds;
    }

    private void RefreshHud()
    {
        if (_hud == null)
        {
            return;
        }

        StageDefinition stage = config.GetStage(_currentStageIndex);
        string stageName = stage != null ? stage.stageName : "标准闯关";
        string stageLabel = "第 " + (_currentStageIndex + 1) + "/" + (_selectedDifficultyStageIndex + 1) + " 关 · " + stageName;

        _hud.SetStageProgressionStats(
            stageLabel,
            _remainingSeconds,
            _totalScore,
            _stageCorrectCount,
            _stageTargetCount);
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
        string binName = result.Bin != null ? result.Bin.DisplayName : "目标垃圾桶";
        return itemName + " 已正确投入 " + binName;
    }

    private static string BuildWrongDetail(ClassificationResult result)
    {
        string itemName = result.Item != null ? result.Item.ItemName : "物品";
        string correctCategory = WasteCategoryText.Format(result.CorrectCategory);
        string reason = string.IsNullOrWhiteSpace(result.Reason) ? "分类不匹配" : result.Reason;
        return itemName + " 应投入 " + correctCategory + "。原因：" + reason;
    }

    private static string BuildStageAccuracy(int correctCount, int wrongCount)
    {
        int total = correctCount + wrongCount;
        if (total <= 0)
        {
            return "本关暂无分类记录";
        }

        float accuracy = (float)correctCount / total;
        return "本关正确率 " + (accuracy * 100f).ToString("0.0") + "%";
    }

    private static string BuildDefaultNextStagePreview(StageDefinition nextStage)
    {
        if (nextStage == null)
        {
            return string.Empty;
        }

        return "目标 " + nextStage.targetCount + " 件，限时 " + Mathf.CeilToInt(nextStage.timeLimitSeconds) + " 秒";
    }

    private void EnterStageTransition(StageDefinition completedStage, int nextStageIndex, float stageElapsed)
    {
        StageDefinition nextStage = config.GetStage(nextStageIndex);
        if (nextStage == null)
        {
            _allStagesCleared = true;
            EndRunSuccess();
            return;
        }

        _currentStageIndex = nextStageIndex;
        _isInTransition = true;
        _transitionRemainingSeconds = config.StageTransitionSeconds;
        _feedbackRemainingSeconds = 0f;

        if (spawner != null)
        {
            spawner.ClearSpawnedItems();
        }

        _hud.HideFeedback();
        SetPlayerInputEnabled(false);

        if (_transitionView != null)
        {
            string completedStageName = completedStage != null ? completedStage.stageName : ("第 " + nextStageIndex + " 关");
            string stageSummary = BuildTransitionSummary(stageElapsed);
            string nextStagePreview = !string.IsNullOrWhiteSpace(nextStage.nextStagePreview)
                ? nextStage.nextStagePreview
                : BuildDefaultNextStagePreview(nextStage);

            _transitionView.Show(
                completedStageName,
                stageSummary,
                nextStage.stageName,
                nextStagePreview,
                nextStageIndex + 1,
                _selectedDifficultyStageIndex + 1);
        }
    }

    private string BuildTransitionSummary(float stageElapsed)
    {
        return BuildStageAccuracy(_stageCorrectCount, _stageWrongCount)
            + "\n正确 " + _stageCorrectCount + "/" + _stageTargetCount
            + "    用时 " + Mathf.CeilToInt(stageElapsed) + " 秒"
            + "    当前总分 " + _totalScore;
    }
}

public sealed class StageRunRecord
{
    public StageRunRecord(
        int stageIndex,
        string stageId,
        string stageName,
        int correctCount,
        int wrongCount,
        float elapsedSeconds,
        bool isCleared)
    {
        StageIndex = stageIndex;
        StageId = stageId;
        StageName = stageName;
        CorrectCount = correctCount;
        WrongCount = wrongCount;
        ElapsedSeconds = elapsedSeconds;
        IsCleared = isCleared;
    }

    public int StageIndex { get; }
    public string StageId { get; }
    public string StageName { get; }
    public int CorrectCount { get; }
    public int WrongCount { get; }
    public float ElapsedSeconds { get; }
    public bool IsCleared { get; }
}

public sealed class StageProgressionSessionStats
{
    public string MostMistakenItemName { get; private set; }
    public int MostMistakenItemCount { get; private set; }

    public static StageProgressionSessionStats FromRecords(
        IReadOnlyList<ClassificationRecord> records,
        IReadOnlyList<StageRunRecord> stageRecords)
    {
        StageProgressionSessionStats stats = new StageProgressionSessionStats();
        if (records == null)
        {
            return stats;
        }

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

    public string BuildSummaryText(bool allCleared, string difficultyName, int clearedStageCount, int failedStageIndex)
    {
        string label = string.IsNullOrWhiteSpace(difficultyName) ? "当前难度" : difficultyName;

        if (allCleared)
        {
            return "已完成「" + label + "」挑战，共通关 " + Mathf.Max(1, clearedStageCount) + " 关。";
        }

        if (!string.IsNullOrWhiteSpace(MostMistakenItemName))
        {
            string stageText = failedStageIndex >= 0 ? "止步第 " + (failedStageIndex + 1) + " 关。" : "未达成目标。";
            return "「" + label + "」" + stageText + "最容易混淆的是 " + MostMistakenItemName + "，建议重点复习后再挑战。";
        }

        if (failedStageIndex >= 0)
        {
            return "「" + label + "」止步第 " + (failedStageIndex + 1) + " 关，可调整节奏后再次尝试。";
        }

        return "「" + label + "」未达成目标，可调整节奏后再次尝试。";
    }
}
