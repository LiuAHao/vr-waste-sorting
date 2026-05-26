using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EndlessScoreModeController : MonoBehaviour
{
    private const float FeedbackDurationSeconds = 1.4f;
    private const float StageOneTimeLimitSeconds = 60f;
    private const float StageTwoTimeLimitSeconds = 120f;
    private const int StageOneRequiredCorrect = 2;
    private const int StageTwoRequiredCorrect = 5;
    private const int StageThreeWrongLimit = 5;
    private const int ScorePerCorrect = 100;
    private const int PenaltyPerWrong = 25;

    private readonly List<Player> _players = new List<Player>();
    private readonly List<EndlessDifficultyStage> _stages = new List<EndlessDifficultyStage>();
    private readonly Dictionary<string, int> _mistakesByItemName = new Dictionary<string, int>();

    private EndlessScoreSpawner _spawner;
    private WasteHudView _hud;
    private WasteResultView _resultView;
    private WasteAnalyticsTracker _analytics;
    private Action _restartAction;

    private float _totalElapsedSeconds;
    private float _stageRemainingSeconds;
    private float _feedbackRemainingSeconds;
    private int _currentStageIndex;
    private int _highestStageIndex;
    private int _score;
    private int _correctCount;
    private int _wrongCount;
    private int _stageCorrectCount;
    private int _stageWrongCount;
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

    public void StartEndless()
    {
        if (_isSessionActive || _isFinished || _hud == null || _resultView == null || _analytics == null)
        {
            return;
        }

        CachePlayers();
        ResetSessionState();
        _analytics.BeginSession();
        _spawner.Initialize(CurrentStage);

        if (_spawner.ActiveItems.Count <= 0)
        {
            Debug.LogWarning("EndlessScoreModeController: 未检测到可用于无尽刷分的垃圾物品。");
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

    public void Tick(float deltaTime)
    {
        if (!_isSessionActive || _isFinished)
        {
            return;
        }

        _totalElapsedSeconds += deltaTime;
        if (CurrentStageHasTimeLimit)
        {
            _stageRemainingSeconds -= deltaTime;
        }

        if (_feedbackRemainingSeconds > 0f)
        {
            _feedbackRemainingSeconds -= deltaTime;
            if (_feedbackRemainingSeconds <= 0f)
            {
                _hud.HideFeedback();
            }
        }

        if (CurrentStageHasTimeLimit && _stageRemainingSeconds <= 0f)
        {
            _stageRemainingSeconds = 0f;
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
            _stageCorrectCount++;
            _currentCombo++;
            _highestCombo = Mathf.Max(_highestCombo, _currentCombo);
            _score += ScorePerCorrect;
            ShowFeedback(true, "投放正确", BuildCorrectDetail(result));
        }
        else
        {
            _wrongCount++;
            _stageWrongCount++;
            _currentCombo = 0;
            _score -= PenaltyPerWrong;
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

        ApplyStageRules();
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

        WasteSessionSummary summary = _analytics.BuildSummary(
            _correctCount + _wrongCount,
            _correctCount,
            _wrongCount,
            _score,
            _totalElapsedSeconds,
            CurrentStageHasTimeLimit ? ResolveCurrentStageTimeLimit() : _totalElapsedSeconds,
            isTimedChallenge: true,
            modeName: "无尽刷分",
            mostMistakenItemName: MostMistakenItemName,
            mostMistakenItemCount: MostMistakenItemCount,
            totalProcessedCount: _correctCount + _wrongCount,
            mistakeSummaryText: BuildSummaryText());

        _resultView.Show(summary, _restartAction);
    }

    private EndlessDifficultyStage CurrentStage => _stages[Mathf.Clamp(_currentStageIndex, 0, _stages.Count - 1)];
    private bool CurrentStageHasTimeLimit => _currentStageIndex < 2;

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
            int index = Mathf.Clamp(_highestStageIndex, 0, _stages.Count - 1);
            EndlessDifficultyStage stage = _stages[index];
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

    private void ApplyStageRules()
    {
        if (_currentStageIndex == 0 && _stageCorrectCount >= StageOneRequiredCorrect)
        {
            AdvanceStage();
            return;
        }

        if (_currentStageIndex == 1 && _stageCorrectCount >= StageTwoRequiredCorrect)
        {
            AdvanceStage();
            return;
        }

        if (_currentStageIndex >= 2 && _stageWrongCount >= StageThreeWrongLimit)
        {
            EndSession();
        }
    }

    private void AdvanceStage()
    {
        _currentStageIndex = Mathf.Min(_currentStageIndex + 1, _stages.Count - 1);
        _highestStageIndex = Mathf.Max(_highestStageIndex, _currentStageIndex);
        _stageCorrectCount = 0;
        _stageWrongCount = 0;
        _stageRemainingSeconds = ResolveCurrentStageTimeLimit();
        ShowFeedback(true, "阶段提升", BuildStageHudName());
        _spawner.RefreshForStage(CurrentStage);
    }

    private void ResetSessionState()
    {
        _totalElapsedSeconds = 0f;
        _currentStageIndex = 0;
        _highestStageIndex = 0;
        _stageRemainingSeconds = ResolveCurrentStageTimeLimit();
        _feedbackRemainingSeconds = 0f;
        _score = 0;
        _correctCount = 0;
        _wrongCount = 0;
        _stageCorrectCount = 0;
        _stageWrongCount = 0;
        _currentCombo = 0;
        _highestCombo = 0;
        _mistakesByItemName.Clear();
        _isFinished = false;
    }

    private float ResolveCurrentStageTimeLimit()
    {
        if (_currentStageIndex == 0)
        {
            return StageOneTimeLimitSeconds;
        }

        if (_currentStageIndex == 1)
        {
            return StageTwoTimeLimitSeconds;
        }

        return 0f;
    }

    private string BuildStageHudName()
    {
        if (_currentStageIndex == 0)
        {
            return CurrentStageName + "  正确 " + _stageCorrectCount + "/" + StageOneRequiredCorrect;
        }

        if (_currentStageIndex == 1)
        {
            return CurrentStageName + "  正确 " + _stageCorrectCount + "/" + StageTwoRequiredCorrect;
        }

        return CurrentStageName + "  错误 " + _stageWrongCount + "/" + StageThreeWrongLimit;
    }

    private string FormatStageTime()
    {
        return CurrentStageHasTimeLimit ? FormatTime(_stageRemainingSeconds) : "不限时";
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
        _hud.SetEndlessScoreStats(FormatStageTime(), _score, processedCount, accuracy, BuildStageHudName(), _currentCombo);
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
        if (_stages.Count > 0)
        {
            return;
        }

        _stages.Add(new EndlessDifficultyStage
        {
            stageName = "阶段 1：基础垃圾",
            activeGarbageCount = 6,
            availableGarbageItemIds = new List<string>
            {
                "garbage_plastic_bottle",
                "garbage_cardboard_box",
                "garbage_leftover_rice",
                "garbage_fruit_peel"
            }
        });

        _stages.Add(new EndlessDifficultyStage
        {
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
        });

        _stages.Add(new EndlessDifficultyStage
        {
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
        });
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
