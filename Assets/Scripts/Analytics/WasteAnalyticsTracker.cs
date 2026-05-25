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
            sessionTime));
    }

    public WasteSessionSummary BuildSummary(
        int totalTargets,
        int correctCount,
        int wrongCount,
        int score,
        float elapsedSeconds,
        float timeLimitSeconds,
        string modeName = null,
        string mostMistakenItemName = null,
        int mostMistakenItemCount = 0,
        int totalProcessedCount = -1,
        string mistakeSummaryText = null)
    {
        return new WasteSessionSummary(
            totalTargets,
            correctCount,
            wrongCount,
            score,
            elapsedSeconds,
            timeLimitSeconds,
            _records,
            modeName,
            mostMistakenItemName,
            mostMistakenItemCount,
            totalProcessedCount,
            mistakeSummaryText);
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
        float sessionTime)
    {
        ItemId = itemId;
        ItemName = itemName;
        SelectedBinName = selectedBinName;
        CorrectCategory = correctCategory;
        SelectedCategory = selectedCategory;
        IsCorrect = isCorrect;
        Reason = reason;
        SessionTime = sessionTime;
    }

    public string ItemId { get; }
    public string ItemName { get; }
    public string SelectedBinName { get; }
    public WasteCategory CorrectCategory { get; }
    public WasteCategory SelectedCategory { get; }
    public bool IsCorrect { get; }
    public string Reason { get; }
    public float SessionTime { get; }
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
        string modeName = null,
        string mostMistakenItemName = null,
        int mostMistakenItemCount = 0,
        int totalProcessedCount = -1,
        string mistakeSummaryText = null)
    {
        TotalTargets = totalTargets;
        CorrectCount = correctCount;
        WrongCount = wrongCount;
        Score = score;
        ElapsedSeconds = elapsedSeconds;
        TimeLimitSeconds = timeLimitSeconds;
        _records = records;
        ModeName = modeName;
        MostMistakenItemName = mostMistakenItemName;
        MostMistakenItemCount = mostMistakenItemCount;
        TotalProcessedCount = totalProcessedCount >= 0 ? totalProcessedCount : correctCount + wrongCount;
        MistakeSummaryText = mistakeSummaryText;
    }

    public int TotalTargets { get; }
    public int CorrectCount { get; }
    public int WrongCount { get; }
    public int Score { get; }
    public float ElapsedSeconds { get; }
    public float TimeLimitSeconds { get; }
    public string ModeName { get; }
    public string MostMistakenItemName { get; }
    public int MostMistakenItemCount { get; }
    public int TotalProcessedCount { get; }
    public string MistakeSummaryText { get; }
    public float Accuracy => (CorrectCount + WrongCount) <= 0 ? 0f : (float)CorrectCount / (CorrectCount + WrongCount);
    public IReadOnlyList<ClassificationRecord> Records => _records;
    public bool IsTimedChallenge => !string.IsNullOrWhiteSpace(ModeName) && ModeName.Contains("限时");
}
