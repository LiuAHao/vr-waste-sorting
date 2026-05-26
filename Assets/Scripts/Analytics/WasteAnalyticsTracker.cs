using System.Collections.Generic;

public sealed class WasteAnalyticsTracker
{
    private readonly List<ClassificationRecord> _records = new List<ClassificationRecord>();

    public IReadOnlyList<ClassificationRecord> Records => _records;

    public void BeginSession()
    {
        _records.Clear();
    }

    public void RecordClassification(ClassificationResult result, float sessionTime)
    {
        RecordClassification(result, sessionTime, -1, null);
    }

    public void RecordClassification(ClassificationResult result, float sessionTime, int stageIndex, string stageName)
    {
        if (result == null)
        {
            return;
        }

        _records.Add(new ClassificationRecord(
            result.Item != null ? result.Item.ItemId : string.Empty,
            result.Item != null ? result.Item.ItemName : string.Empty,
            result.Bin != null ? result.Bin.DisplayName : string.Empty,
            result.CorrectCategory,
            result.SelectedCategory,
            result.IsCorrect,
            result.Reason,
            sessionTime,
            stageIndex,
            stageName));
    }

    public WasteSessionSummary BuildSummary(
        int totalTargets,
        int correctCount,
        int wrongCount,
        int score,
        float elapsedSeconds,
        float timeLimitSeconds,
        bool isTimedChallenge = false,
        string modeName = null,
        string mostMistakenItemName = null,
        int mostMistakenItemCount = 0,
        int totalProcessedCount = -1,
        string mistakeSummaryText = null,
        bool isStageProgression = false,
        int clearedStageCount = 0,
        int failedStageIndex = -1,
        bool allStagesCleared = false,
        string selectedDifficultyName = null)
    {
        return new WasteSessionSummary(
            totalTargets,
            correctCount,
            wrongCount,
            score,
            elapsedSeconds,
            timeLimitSeconds,
            _records,
            isTimedChallenge,
            modeName,
            mostMistakenItemName,
            mostMistakenItemCount,
            totalProcessedCount,
            mistakeSummaryText,
            isStageProgression,
            clearedStageCount,
            failedStageIndex,
            allStagesCleared,
            selectedDifficultyName);
    }
}

public sealed class ClassificationRecord
{
    public ClassificationRecord(
        string itemId,
        string itemName,
        string selectedBinName,
        WasteCategory correctCategory,
        WasteCategory selectedCategory,
        bool isCorrect,
        string reason,
        float sessionTime,
        int stageIndex = -1,
        string stageName = null)
    {
        ItemId = itemId;
        ItemName = itemName;
        SelectedBinName = selectedBinName;
        CorrectCategory = correctCategory;
        SelectedCategory = selectedCategory;
        IsCorrect = isCorrect;
        Reason = reason;
        SessionTime = sessionTime;
        StageIndex = stageIndex;
        StageName = stageName;
    }

    public string ItemId { get; }
    public string ItemName { get; }
    public string SelectedBinName { get; }
    public WasteCategory CorrectCategory { get; }
    public WasteCategory SelectedCategory { get; }
    public bool IsCorrect { get; }
    public string Reason { get; }
    public float SessionTime { get; }
    public int StageIndex { get; }
    public string StageName { get; }
}

public sealed class WasteSessionSummary
{
    private readonly IReadOnlyList<ClassificationRecord> _records;

    public WasteSessionSummary(
        int totalTargets,
        int correctCount,
        int wrongCount,
        int score,
        float elapsedSeconds,
        float timeLimitSeconds,
        IReadOnlyList<ClassificationRecord> records,
        bool isTimedChallenge = false,
        string modeName = null,
        string mostMistakenItemName = null,
        int mostMistakenItemCount = 0,
        int totalProcessedCount = -1,
        string mistakeSummaryText = null,
        bool isStageProgression = false,
        int clearedStageCount = 0,
        int failedStageIndex = -1,
        bool allStagesCleared = false,
        string selectedDifficultyName = null)
    {
        TotalTargets = totalTargets;
        CorrectCount = correctCount;
        WrongCount = wrongCount;
        Score = score;
        ElapsedSeconds = elapsedSeconds;
        TimeLimitSeconds = timeLimitSeconds;
        _records = records;
        IsTimedChallenge = isTimedChallenge;
        IsStageProgression = isStageProgression;
        ModeName = modeName;
        MostMistakenItemName = mostMistakenItemName;
        MostMistakenItemCount = mostMistakenItemCount;
        TotalProcessedCount = totalProcessedCount >= 0 ? totalProcessedCount : correctCount + wrongCount;
        MistakeSummaryText = mistakeSummaryText;
        ClearedStageCount = clearedStageCount;
        FailedStageIndex = failedStageIndex;
        AllStagesCleared = allStagesCleared;
        SelectedDifficultyName = selectedDifficultyName;
    }

    public int TotalTargets { get; }
    public int CorrectCount { get; }
    public int WrongCount { get; }
    public int Score { get; }
    public float ElapsedSeconds { get; }
    public float TimeLimitSeconds { get; }
    public bool IsTimedChallenge { get; }
    public bool IsStageProgression { get; }
    public string ModeName { get; }
    public string MostMistakenItemName { get; }
    public int MostMistakenItemCount { get; }
    public int TotalProcessedCount { get; }
    public string MistakeSummaryText { get; }
    public int ClearedStageCount { get; }
    public int FailedStageIndex { get; }
    public bool AllStagesCleared { get; }
    public string SelectedDifficultyName { get; }
    public float Accuracy => (CorrectCount + WrongCount) <= 0 ? 0f : (float)CorrectCount / (CorrectCount + WrongCount);
    public IReadOnlyList<ClassificationRecord> Records => _records;
}
