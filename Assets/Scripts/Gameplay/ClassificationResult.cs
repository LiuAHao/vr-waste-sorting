public class ClassificationResult
{
    public GarbageItem Item { get; }
    public TrashBin Bin { get; }
    public bool IsCorrect { get; }
    public WasteCategory CorrectCategory { get; }
    public WasteCategory SelectedCategory { get; }
    public string Reason { get; }

    public ClassificationResult(
        GarbageItem item,
        TrashBin bin,
        bool isCorrect,
        WasteCategory correctCategory,
        WasteCategory selectedCategory,
        string reason)
    {
        Item = item;
        Bin = bin;
        IsCorrect = isCorrect;
        CorrectCategory = correctCategory;
        SelectedCategory = selectedCategory;
        Reason = reason;
    }
}
