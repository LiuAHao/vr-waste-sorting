using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EndlessScoreModeController : MonoBehaviour
{
    private const float FeedbackDurationSeconds = 1.4f;

    private readonly List<Player> _players = new List<Player>();
    private readonly Dictionary<string, int> _mistakesByItemName = new Dictionary<string, int>();

    [SerializeField] private float totalTimeLimitSeconds = 180f;
    [SerializeField] private int scorePerCorrect = 100;
    [SerializeField] private int penaltyPerWrong = 25;
    [SerializeField] private List<EndlessDifficultyStage> stages = new List<EndlessDifficultyStage>();

    private EndlessScoreSpawner _spawner;
    private WasteHudView _hud;
    private WasteResultView _resultView;
    private WastePauseView _pauseView;
    private WasteAnalyticsTracker _analytics;
    private Action _restartAction;
    private Action _returnToMenuAction;

    private float _totalElapsedSeconds;
    private float _feedbackRemainingSeconds;
    private int _currentStageIndex;
    private int _highestStageIndex;
    private int _score;
    private int _correctCount;
    private int _wrongCount;
    private int _currentCombo;
    private int _highestCombo;
    private bool _isSessionActive;
    private bool _isFinished;

    public bool IsSessionActive => _isSessionActive && !_isFinished;

    public void Configure(WasteHudView hud, WasteResultView resultView, WasteAnalyticsTracker analytics, Action restartAction)
    {
        _hud = hud;
        _resultView = resultView;
        _analytics = analytics;
        _restartAction = restartAction;

        _spawner = GetComponent<EndlessScoreSpawner>();
        if (_spawner == null)
        {
            _spawner = gameObject.AddComponent<EndlessScoreSpawner>();
        }

        EnsureStages();
    }

    public void ConfigurePauseView(WastePauseView pauseView, Action returnToMenuAction)
    {
        _pauseView = pauseView;
        _returnToMenuAction = returnToMenuAction;
    }

    public void StartEndless()
    {
        if (_isSessionActive || _isFinished || _hud == null || _resultView == null || _analytics == null)
        {
            return;
        }

        Time.timeScale = 1f;
        _pauseView?.Hide();
        CachePlayers();
        ResetSessionState();
        _analytics.BeginSession();
        _spawner.Initialize(CurrentStage);

        if (_spawner.ActiveItems.Count <= 0)
        {
            Debug.LogWarning("EndlessScoreModeController: 未检测到可用于生存刷分的垃圾物品。");
            _spawner.RestoreScene();
            return;
        }

        _isSessionActive = true;
        _resultView.Hide();
        _hud.SetVisible(true);
        _hud.HideFeedback();
        SetPlayerInputEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        RefreshHud();
    }

    public void AbortSession()
    {
        _isSessionActive = false;
        _isFinished = false;
        _feedbackRemainingSeconds = 0f;
        Time.timeScale = 1f;
        _pauseView?.Hide();
        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _hud?.HideFeedback();
        _spawner?.RestoreScene();
    }

    public void Tick(float deltaTime)
    {
        if (!_isSessionActive || _isFinished)
        {
            return;
        }

        _totalElapsedSeconds += deltaTime;
        int targetStageIndex = ResolveStageIndexForElapsed(_totalElapsedSeconds);
        if (targetStageIndex != _currentStageIndex)
        {
            AdvanceStage(targetStageIndex);
        }

        if (_feedbackRemainingSeconds > 0f)
        {
            _feedbackRemainingSeconds -= deltaTime;
            if (_feedbackRemainingSeconds <= 0f)
            {
                _hud.HideFeedback();
            }
        }

        if (RemainingSeconds <= 0f)
        {
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

        _analytics.RecordClassification(result, _totalElapsedSeconds);

        if (result.IsCorrect)
        {
            _correctCount++;
            _currentCombo++;
            _highestCombo = Mathf.Max(_highestCombo, _currentCombo);
            _score += scorePerCorrect;
            ShowFeedback(true, "投放正确", BuildCorrectDetail(result));
        }
        else
        {
            _wrongCount++;
            _currentCombo = 0;
            _score -= penaltyPerWrong;
            TrackMistake(result);
            ShowFeedback(false, "投放错误", BuildWrongDetail(result));
            if (result.Item != null)
            {
                result.Item.MarkCompleted();
            }
        }

        if (result.Item != null)
        {
            _spawner.HandleItemProcessed(result.Item, CurrentStage);
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
        _pauseView?.Hide();
        _spawner.RestoreScene();

        WasteSessionSummary summary = _analytics.BuildSummary(
            _correctCount + _wrongCount,
            _correctCount,
            _wrongCount,
            _score,
            _totalElapsedSeconds,
            totalTimeLimitSeconds,
            isTimedChallenge: true,
            modeName: "生存刷分",
            mostMistakenItemName: MostMistakenItemName,
            mostMistakenItemCount: MostMistakenItemCount,
            totalProcessedCount: _correctCount + _wrongCount,
            mistakeSummaryText: BuildSummaryText());

        _analytics.RecordSessionSummary(new WasteSessionSummary(summary));
        _resultView.Show(summary, _restartAction);
    }

    public void TogglePause()
    {
        if (!_isSessionActive || _isFinished || _pauseView == null)
        {
            return;
        }

        if (Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            SetPlayerInputEnabled(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _pauseView.Show(ResumeFromPause, EndPauseToMenu);
        }
        else
        {
            ResumeFromPause();
        }
    }

    private EndlessDifficultyStage CurrentStage => stages[Mathf.Clamp(_currentStageIndex, 0, stages.Count - 1)];
    private float RemainingSeconds => Mathf.Max(0f, totalTimeLimitSeconds - _totalElapsedSeconds);

    private string CurrentStageName
    {
        get
        {
            EndlessDifficultyStage stage = CurrentStage;
            return stage != null && !string.IsNullOrWhiteSpace(stage.stageName) ? stage.stageName : "阶段 " + (_currentStageIndex + 1);
        }
    }

    private string HighestStageName
    {
        get
        {
            int index = Mathf.Clamp(_highestStageIndex, 0, stages.Count - 1);
            EndlessDifficultyStage stage = stages[index];
            return stage != null && !string.IsNullOrWhiteSpace(stage.stageName) ? stage.stageName : "阶段 " + (index + 1);
        }
    }

    private string MostMistakenItemName
    {
        get
        {
            string itemName = string.Empty;
            int count = 0;
            foreach (KeyValuePair<string, int> pair in _mistakesByItemName)
            {
                if (pair.Value > count)
                {
                    itemName = pair.Key;
                    count = pair.Value;
                }
            }

            return itemName;
        }
    }

    private int MostMistakenItemCount
    {
        get
        {
            int count = 0;
            foreach (KeyValuePair<string, int> pair in _mistakesByItemName)
            {
                count = Mathf.Max(count, pair.Value);
            }

            return count;
        }
    }

    private void AdvanceStage(int targetStageIndex)
    {
        _currentStageIndex = Mathf.Clamp(targetStageIndex, 0, stages.Count - 1);
        _highestStageIndex = Mathf.Max(_highestStageIndex, _currentStageIndex);
        ShowFeedback(true, "阶段提升", CurrentStageName);
        _spawner.RefreshForStage(CurrentStage);
    }

    private void ResetSessionState()
    {
        _totalElapsedSeconds = 0f;
        _currentStageIndex = 0;
        _highestStageIndex = 0;
        _feedbackRemainingSeconds = 0f;
        _score = 0;
        _correctCount = 0;
        _wrongCount = 0;
        _currentCombo = 0;
        _highestCombo = 0;
        _mistakesByItemName.Clear();
        _isFinished = false;
    }

    private string BuildStageHudName()
    {
        return CurrentStageName;
    }

    private string BuildSummaryText()
    {
        string baseText = "最高阶段：" + HighestStageName + "。最高连击：" + _highestCombo + "。";
        if (!string.IsNullOrWhiteSpace(MostMistakenItemName))
        {
            return baseText + "最容易出错的是 " + MostMistakenItemName + "。";
        }

        return baseText + "本轮没有明显错误热点。";
    }

    private void TrackMistake(ClassificationResult result)
    {
        string itemName = result.Item != null && !string.IsNullOrWhiteSpace(result.Item.ItemName) ? result.Item.ItemName : "未知物品";
        if (!_mistakesByItemName.ContainsKey(itemName))
        {
            _mistakesByItemName[itemName] = 0;
        }

        _mistakesByItemName[itemName]++;
    }

    private void ShowFeedback(bool isCorrect, string title, string detail)
    {
        _hud.ShowFeedback(isCorrect, title, detail);
        _feedbackRemainingSeconds = FeedbackDurationSeconds;
    }

    private void RefreshHud()
    {
        int processedCount = _correctCount + _wrongCount;
        float accuracy = processedCount <= 0 ? 0f : (float)_correctCount / processedCount;
        _hud.SetEndlessScoreStats(FormatTime(RemainingSeconds), _score, processedCount, accuracy, BuildStageHudName(), _currentCombo);
    }

    private void ResumeFromPause()
    {
        Time.timeScale = 1f;
        _pauseView?.Hide();
        SetPlayerInputEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EndPauseToMenu()
    {
        Time.timeScale = 1f;
        _pauseView?.Hide();
        _returnToMenuAction?.Invoke();
    }

    private void CachePlayers()
    {
        _players.Clear();
        _players.AddRange(FindObjectsOfType<Player>());
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

    private void EnsureStages()
    {
        if (stages.Count <= 0)
        {
            stages = new List<EndlessDifficultyStage>
            {
                new EndlessDifficultyStage
                {
                    startSecond = 0f,
                    stageName = "阶段 1：基础垃圾",
                    activeGarbageCount = 6,
                    availableGarbageItemIds = new List<string>
                    {
                        "garbage_plastic_bottle",
                        "garbage_cardboard_box",
                        "garbage_leftover_rice",
                        "garbage_fruit_peel"
                    }
                },
                new EndlessDifficultyStage
                {
                    startSecond = 60f,
                    stageName = "阶段 2：混合垃圾",
                    activeGarbageCount = 7,
                    availableGarbageItemIds = new List<string>
                    {
                        "garbage_plastic_bottle",
                        "garbage_cardboard_box",
                        "garbage_aluminum_can",
                        "garbage_leftover_rice",
                        "garbage_fruit_peel",
                        "garbage_vegetable_leaf",
                        "garbage_dirty_tissue",
                        "garbage_milk_tea_cup"
                    }
                },
                new EndlessDifficultyStage
                {
                    startSecond = 120f,
                    stageName = "阶段 3：易混淆挑战",
                    activeGarbageCount = 8,
                    availableGarbageItemIds = new List<string>
                    {
                        "garbage_plastic_bottle",
                        "garbage_milk_tea_cup",
                        "garbage_oily_takeout_box",
                        "garbage_dirty_tissue",
                        "garbage_battery",
                        "garbage_expired_medicine",
                        "garbage_lamp_tube",
                        "garbage_aluminum_can"
                    }
                }
            };
        }

        stages.Sort((left, right) => left.startSecond.CompareTo(right.startSecond));
    }

    private int ResolveStageIndexForElapsed(float elapsedSeconds)
    {
        int stageIndex = 0;
        for (int i = 0; i < stages.Count; i++)
        {
            EndlessDifficultyStage stage = stages[i];
            if (stage != null && elapsedSeconds >= stage.startSecond)
            {
                stageIndex = i;
            }
        }

        return stageIndex;
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

    private static string FormatTime(float seconds)
    {
        if (seconds < 0f)
        {
            seconds = 0f;
        }

        int wholeSeconds = Mathf.CeilToInt(seconds);
        int minutes = wholeSeconds / 60;
        int remainder = wholeSeconds % 60;
        return minutes.ToString("00") + ":" + remainder.ToString("00");
    }
}
