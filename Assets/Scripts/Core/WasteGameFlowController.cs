using System.Collections.Generic;
using UnityEngine;

public sealed class WasteGameFlowController
{
    private const float DefaultTimeLimitSeconds = 180f;
    private const int CorrectScoreValue = 100;
    private const int WrongScorePenalty = 25;
    private const float FeedbackDurationSeconds = 1.4f;

    private readonly List<GarbageItem> _items = new List<GarbageItem>();
    private readonly List<Player> _players = new List<Player>();

    private WasteAnalyticsTracker _analytics;
    private WasteHudView _hud;
    private WasteResultView _resultView;
    private System.Action _restartAction;

    private float _timeLimitSeconds = DefaultTimeLimitSeconds;
    private float _remainingSeconds;
    private float _feedbackRemainingSeconds;
    private int _score;
    private int _correctCount;
    private int _wrongCount;
    private bool _hasActiveSession;
    private bool _isFinished;

    public void BindScene(WasteHudView hud, WasteResultView resultView, WasteAnalyticsTracker analytics, System.Action restartAction)
    {
        _hud = hud;
        _resultView = resultView;
        _analytics = analytics;
        _restartAction = restartAction;

        _items.Clear();
        _items.AddRange(Object.FindObjectsOfType<GarbageItem>());

        _players.Clear();
        _players.AddRange(Object.FindObjectsOfType<Player>());

        if (_items.Count <= 0)
        {
            _hasActiveSession = false;
            _isFinished = false;
            _hud.SetVisible(false);
            _hud.HideFeedback();
            _resultView.Hide();
            return;
        }

        _timeLimitSeconds = DefaultTimeLimitSeconds;
        _remainingSeconds = _timeLimitSeconds;
        _feedbackRemainingSeconds = 0f;
        _score = 0;
        _correctCount = 0;
        _wrongCount = 0;
        _isFinished = false;
        _hasActiveSession = true;

        _analytics.BeginSession();
        SetPlayerInputEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _hud.SetVisible(true);
        _hud.HideFeedback();
        _resultView.Hide();
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

        if (_correctCount >= _items.Count)
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
                result.Item.ResetToStartPosition();
            }
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
        _hasActiveSession = false;
        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _hud.HideFeedback();

        WasteSessionSummary summary = _analytics.BuildSummary(
            _items.Count,
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
        _hud.SetStats(_remainingSeconds, _score, _correctCount, _items.Count);
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
        string reason = string.IsNullOrWhiteSpace(result.Reason) ? "分类不匹配" : result.Reason;
        return itemName + " 需要调整。原因: " + reason;
    }
}
