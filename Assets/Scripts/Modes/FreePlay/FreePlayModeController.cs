using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FreePlayModeController : MonoBehaviour
{
    private const float FeedbackDurationSeconds = 1.4f;
    private const int TargetActiveItemCount = 15;

    [SerializeField] private StageGarbageSpawner spawner;

    private WasteHudView _hud;
    private WasteResultView _resultView;
    private WastePauseView _pauseView;
    private WasteAnalyticsTracker _analytics;
    private Action _returnToMenuAction;
    private bool _sessionSummaryRecorded;

    private readonly List<Player> _players = new List<Player>();
    private readonly List<GarbageItem> _hiddenSceneItems = new List<GarbageItem>();

    private float _feedbackRemainingSeconds;
    private int _score;
    private int _correctCount;
    private int _wrongCount;
    private int _processedCount;
    private bool _isSessionActive;
    private bool _isFinished;

    public bool IsSessionActive => _isSessionActive && !_isFinished;

    public void Configure(StageGarbageSpawner stageSpawner)
    {
        if (stageSpawner != null)
        {
            spawner = stageSpawner;
        }
    }

    public void Configure(WasteHudView hud, WasteResultView resultView, WasteAnalyticsTracker analytics, Action returnToMenuAction)
    {
        _hud = hud;
        _resultView = resultView;
        _analytics = analytics;
        _returnToMenuAction = returnToMenuAction;
    }

    public void ConfigurePauseView(WastePauseView pauseView)
    {
        _pauseView = pauseView;
    }

    public void StartFreePlay()
    {
        if (_isSessionActive || _isFinished || spawner == null)
        {
            if (spawner == null)
            {
                Debug.LogWarning("FreePlayModeController: 未配置 StageGarbageSpawner。");
            }

            return;
        }

        Time.timeScale = 1f;
        _pauseView?.Hide();
        CachePlayers();
        HideSceneGarbage();
        ResetSessionState();

        _analytics?.BeginSession();
        _resultView.Hide();
        _hud.SetVisible(true);
        _hud.HideFeedback();
        SetPlayerInputEnabled(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SpawnUntilFilled();
        _isSessionActive = true;
        _sessionSummaryRecorded = false;
        RefreshHud();
    }

    public void Tick(float deltaTime)
    {
        if (!_isSessionActive || _isFinished)
        {
            return;
        }

        if (_feedbackRemainingSeconds > 0f)
        {
            _feedbackRemainingSeconds -= deltaTime;
            if (_feedbackRemainingSeconds <= 0f)
            {
                _hud.HideFeedback();
            }
        }

        RefreshHud();
    }

    public void HandleClassification(ClassificationResult result)
    {
        if (!_isSessionActive || _isFinished || result == null)
        {
            return;
        }

        _analytics?.RecordClassification(result, _processedCount);

        if (result.IsCorrect)
        {
            _correctCount++;
            _score += 100;
            ShowFeedback(true, "投放正确", BuildCorrectDetail(result));
        }
        else
        {
            _wrongCount++;
            _score -= 25;
            ShowFeedback(false, "投放错误", BuildWrongDetail(result));
            if (result.Item != null)
            {
                result.Item.MarkCompleted();
            }
        }

        if (result.Item != null)
        {
            spawner.HandleItemProcessed(result.Item);
            _processedCount++;
        }

        SpawnUntilFilled();
        RefreshHud();
    }

    public void EndSessionAndReturnToMenu()
    {
        if (!_isSessionActive || _isFinished)
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
        spawner?.ClearSpawnedItems();
        RestoreSceneGarbage();
        RecordSessionSummaryIfNeeded();
        _returnToMenuAction?.Invoke();
    }

    public void AbortSession()
    {
        _isSessionActive = false;
        _isFinished = false;
        _feedbackRemainingSeconds = 0f;
        _sessionSummaryRecorded = false;
        Time.timeScale = 1f;
        SetPlayerInputEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _hud?.HideFeedback();
        _pauseView?.Hide();
        spawner?.ClearSpawnedItems();
        RestoreSceneGarbage();
    }

    public void TogglePause()
    {
        if (!_isSessionActive || _isFinished)
        {
            return;
        }

        if (_pauseView == null)
        {
            return;
        }

        bool paused = Time.timeScale > 0f;
        if (paused)
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

    private void SpawnUntilFilled()
    {
        if (spawner == null)
        {
            return;
        }

        while (spawner.ActiveItems.Count < TargetActiveItemCount)
        {
            int spawned = spawner.SpawnSingleFullMapRandomFromScene();
            if (spawned <= 0)
            {
                break;
            }
        }
    }

    private void ResetSessionState()
    {
        _feedbackRemainingSeconds = 0f;
        _score = 0;
        _correctCount = 0;
        _wrongCount = 0;
        _processedCount = 0;
        _isFinished = false;
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

            if (spawner != null && IsSpawnerManagedItem(item))
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

    private void ShowFeedback(bool isCorrect, string title, string detail)
    {
        _hud.ShowFeedback(isCorrect, title, detail);
        _feedbackRemainingSeconds = FeedbackDurationSeconds;
    }

    private void RefreshHud()
    {
        _hud?.SetFreePlayStats(_score, _processedCount, _correctCount, _wrongCount);
    }

    private WasteSessionSummary BuildSummary()
    {
        int totalProcessed = _correctCount + _wrongCount;
        return new WasteSessionSummary(
            totalProcessed,
            _correctCount,
            _wrongCount,
            _score,
            0f,
            0f,
            _analytics != null ? _analytics.Records : null,
            modeName: "自由模式",
            totalProcessedCount: totalProcessed,
            mistakeSummaryText: "自由模式累计 " + totalProcessed + " 次分类。");
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
        RecordSessionSummaryIfNeeded();
        AbortSession();
        _returnToMenuAction?.Invoke();
    }

    private void RecordSessionSummaryIfNeeded()
    {
        if (_sessionSummaryRecorded)
        {
            return;
        }

        WasteSessionSummary summary = BuildSummary();
        _analytics?.RecordSessionSummary(summary);
        _sessionSummaryRecorded = true;
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
}
