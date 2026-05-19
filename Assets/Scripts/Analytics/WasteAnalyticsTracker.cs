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

    public WasteSessionSummary BuildSummary(int totalTargets, int correctCount, int wrongCount, int score, float elapsedSeconds, float timeLimitSeconds)
    {
        return new WasteSessionSummary(totalTargets, correctCount, wrongCount, score, elapsedSeconds, timeLimitSeconds, _records);
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
        IReadOnlyList<ClassificationRecord> records)
    {
        TotalTargets = totalTargets;
        CorrectCount = correctCount;
        WrongCount = wrongCount;
        Score = score;
        ElapsedSeconds = elapsedSeconds;
        TimeLimitSeconds = timeLimitSeconds;
        _records = records;
    }

    public int TotalTargets { get; }
    public int CorrectCount { get; }
    public int WrongCount { get; }
    public int Score { get; }
    public float ElapsedSeconds { get; }
    public float TimeLimitSeconds { get; }
    public float Accuracy => TotalTargets <= 0 ? 0f : (float)CorrectCount / TotalTargets;
    public IReadOnlyList<ClassificationRecord> Records => _records;
}
