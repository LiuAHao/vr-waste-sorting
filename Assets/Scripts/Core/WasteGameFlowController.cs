using System.Collections.Generic;
using UnityEngine;

public sealed class WasteGameFlowController
{
    private const float DefaultTimeLimitSeconds = 180f;
    private const int DefaultTargetCount = 12;
    private const int CorrectScoreValue = 100;
    private const int WrongScorePenalty = 25;
    private const float FeedbackDurationSeconds = 1.4f;

    private readonly List<GarbageItem> _items = new List<GarbageItem>();
    private readonly List<Player> _players = new List<Player>();

    private WasteAnalyticsTracker _analytics;
    private WasteStartView _startView;
    private WasteHudView _hud;
    private WasteResultView _resultView;
    private System.Action _restartAction;

    private float _timeLimitSeconds = DefaultTimeLimitSeconds;
    private float _remainingSeconds;
    private float _feedbackRemainingSeconds;
    private int _targetCount;
    private int _score;
    private int _correctCount;
    private int _wrongCount;
    private bool _isReadyToStart;
    private bool _hasActiveSession;
    private bool _isFinished;

    public void BindScene(
        WasteStartView startView,
        WasteHudView hud,
        WasteResultView resultView,
        WasteAnalyticsTracker analytics,
        System.Action restartAction,
        System.Action timedChallengeAction = null)
    {
        _startView = startView;
        _hud = hud;
        _resultView = resultView;
        _analytics = analytics;
        _restartAction = restartAction;

        WasteUiFactory.HideLegacySceneUi();

        _items.Clear();
        _items.AddRange(Object.FindObjectsOfType<GarbageItem>());

        _players.Clear();
        _players.AddRange(Object.FindObjectsOfType<Player>());

        if (_items.Count <= 0)
        {
            _isReadyToStart = false;
            _hasActiveSession = false;
            _isFinished = false;
            _startView.Hide();
            _hud.SetVisible(false);
            _hud.HideFeedback();
            _resultView.Hide();
            return;
        }

        WasteGameSceneConfig sceneConfig = Object.FindObjectOfType<WasteGameSceneConfig>();
        _timeLimitSeconds = sceneConfig != null ? sceneConfig.TotalTimeSeconds : DefaultTimeLimitSeconds;
        _targetCount = ResolveTargetCount(sceneConfig, _items.Count);
        _remainingSeconds = _timeLimitSeconds;
        _feedbackRemainingSeconds = 0f;
        _score = 0;
        _correctCount = 0;
        _wrongCount = 0;
        _isReadyToStart = true;
        _isFinished = false;
        _hasActiveSession = false;

        _analytics.BeginSession();
        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _startView.Show(BeginSession, timedChallengeAction);
        _hud.SetVisible(false);
        _hud.HideFeedback();
        _resultView.Hide();
        RefreshHud();
    }

    public void BeginSession()
    {
        if (!_isReadyToStart || _isFinished)
        {
            return;
        }

        _isReadyToStart = false;
        _hasActiveSession = true;
        _startView.Hide();
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
        if (!_hasActiveSession || _isFinished)
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

        if ((_correctCount + _wrongCount) >= _targetCount)
        {
            EndSession();
        }
    }

    public void HandleClassification(ClassificationResult result)
    {
        if (!_hasActiveSession || _isFinished || result == null)
        {
            return;
        }

        float elapsed = _timeLimitSeconds - _remainingSeconds;
        _analytics.RecordClassification(result, elapsed);

        if (result.IsCorrect)
        {
            _correctCount++;
            _score += CorrectScoreValue;
            ShowFeedback(true, "投放正确", BuildCorrectDetail(result));
        }
        else
        {
            _wrongCount++;
            _score -= WrongScorePenalty;
            ShowFeedback(false, "投放错误", BuildWrongDetail(result));
            if (result.Item != null)
            {
                result.Item.MarkCompleted();
            }
        }

        RefreshHud();

        if ((_correctCount + _wrongCount) >= _targetCount)
        {
            EndSession();
        }
    }

    private void EndSession()
    {
        if (_isFinished)
        {
            return;
        }

        _isFinished = true;
        _hasActiveSession = false;
        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _hud.HideFeedback();

        WasteSessionSummary summary = _analytics.BuildSummary(
            _targetCount,
            _correctCount,
            _wrongCount,
            _score,
            _timeLimitSeconds - _remainingSeconds,
            _timeLimitSeconds);

        _resultView.Show(summary, _restartAction);
    }

    private void ShowFeedback(bool isCorrect, string title, string detail)
    {
        _hud.ShowFeedback(isCorrect, title, detail);
        _feedbackRemainingSeconds = FeedbackDurationSeconds;
    }

    private void RefreshHud()
    {
        _hud.SetStats(_remainingSeconds, _score, _correctCount + _wrongCount, _targetCount);
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

    private static int ResolveTargetCount(WasteGameSceneConfig sceneConfig, int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0;
        }

        int configuredTarget = sceneConfig != null ? sceneConfig.TargetCount : DefaultTargetCount;
        return Mathf.Clamp(configuredTarget, 1, itemCount);
    }
}
