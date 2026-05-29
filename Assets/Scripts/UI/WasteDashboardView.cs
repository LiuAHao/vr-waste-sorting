using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class WasteDashboardView
{
    private readonly GameObject _root;
    private readonly Text _titleText;
    private readonly Text _summaryText;
    private readonly Text _totalCountText;
    private readonly Text _correctCountText;
    private readonly Text _wrongCountText;
    private readonly Text _accuracyText;
    private readonly Text _mostWrongItemText;
    private readonly Text _mostWrongCategoryText;
    private readonly Text _topMistakeDetailText;
    private readonly Transform _itemsRoot;
    private readonly Button _closeButton;

    private WasteDashboardView(
        GameObject root,
        Text titleText,
        Text summaryText,
        Text totalCountText,
        Text correctCountText,
        Text wrongCountText,
        Text accuracyText,
        Text mostWrongItemText,
        Text mostWrongCategoryText,
        Text topMistakeDetailText,
        Transform itemsRoot,
        Button closeButton)
    {
        _root = root;
        _titleText = titleText;
        _summaryText = summaryText;
        _totalCountText = totalCountText;
        _correctCountText = correctCountText;
        _wrongCountText = wrongCountText;
        _accuracyText = accuracyText;
        _mostWrongItemText = mostWrongItemText;
        _mostWrongCategoryText = mostWrongCategoryText;
        _topMistakeDetailText = topMistakeDetailText;
        _itemsRoot = itemsRoot;
        _closeButton = closeButton;
    }

    public static WasteDashboardView Create()
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("WasteDashboard");

        Image backdrop = WasteUiFactory.CreatePanel("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.62f));
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image panel = WasteUiFactory.CreatePanel("Panel", backdrop.transform, new Color(0.08f, 0.11f, 0.13f, 0.98f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1280f, 860f);

        Text titleText = WasteUiFactory.CreateText("Title", panel.transform, "数据看板", 58, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
        SetRect(titleText.rectTransform, new Vector2(24f, -74f), new Vector2(-24f, -22f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Text summaryText = WasteUiFactory.CreateText("Summary", panel.transform, string.Empty, 28, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.86f, 0.92f, 0.94f, 1f));
        SetRect(summaryText.rectTransform, new Vector2(36f, -132f), new Vector2(-36f, -92f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        RectTransform metricsRoot = WasteUiFactory.CreateRect("MetricsRoot", panel.transform);
        SetRect(metricsRoot, new Vector2(34f, -252f), new Vector2(-34f, -160f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        GridLayoutGroup metricsGrid = metricsRoot.gameObject.AddComponent<GridLayoutGroup>();
        metricsGrid.cellSize = new Vector2(286f, 92f);
        metricsGrid.spacing = new Vector2(14f, 14f);
        metricsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        metricsGrid.constraintCount = 4;
        metricsGrid.childAlignment = TextAnchor.UpperCenter;

        CreateMetricCard(metricsRoot, "总分类数量", out Text totalCountText);
        CreateMetricCard(metricsRoot, "正确数量", out Text correctCountText);
        CreateMetricCard(metricsRoot, "错误数量", out Text wrongCountText);
        CreateMetricCard(metricsRoot, "正确率", out Text accuracyText);

        RectTransform insightsRoot = WasteUiFactory.CreateRect("InsightsRoot", panel.transform);
        SetRect(insightsRoot, new Vector2(34f, -402f), new Vector2(-34f, -264f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        GridLayoutGroup insightsGrid = insightsRoot.gameObject.AddComponent<GridLayoutGroup>();
        insightsGrid.cellSize = new Vector2(390f, 138f);
        insightsGrid.spacing = new Vector2(14f, 14f);
        insightsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        insightsGrid.constraintCount = 3;
        insightsGrid.childAlignment = TextAnchor.UpperCenter;

        Text mostWrongItemText = CreateInfoBlock(insightsRoot, "最多的错误物品");
        Text mostWrongCategoryText = CreateInfoBlock(insightsRoot, "最常错分类");
        Text topMistakeDetailText = CreateInfoBlock(insightsRoot, "补充说明");

        Image listPanel = WasteUiFactory.CreatePanel("ListPanel", panel.transform, new Color(0.05f, 0.06f, 0.08f, 0.96f));
        RectTransform listPanelRect = listPanel.rectTransform;
        listPanelRect.anchorMin = new Vector2(0f, 0f);
        listPanelRect.anchorMax = new Vector2(1f, 1f);
        listPanelRect.offsetMin = new Vector2(34f, 98f);
        listPanelRect.offsetMax = new Vector2(-34f, -410f);

        Text itemsTitle = WasteUiFactory.CreateText("ItemsTitle", listPanel.transform, "错误条目", 32, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        SetRect(itemsTitle.rectTransform, new Vector2(20f, -44f), new Vector2(-20f, -10f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        ScrollRect scrollRect = listPanel.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        Image viewport = WasteUiFactory.CreatePanel("Viewport", listPanel.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform viewportRect = viewport.rectTransform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(16f, 16f);
        viewportRect.offsetMax = new Vector2(-16f, -56f);
        viewport.gameObject.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportRect;

        RectTransform content = WasteUiFactory.CreateRect("Content", viewport.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        scrollRect.content = content;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 12f;
        layout.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button closeButton = WasteUiFactory.CreateButton("CloseButton", panel.transform, "返回", new Color(0.27f, 0.31f, 0.37f, 1f), Color.white, null);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.sizeDelta = new Vector2(320f, 70f);
        closeRect.anchoredPosition = new Vector2(0f, 24f);

        root.SetActive(false);
        return new WasteDashboardView(root, titleText, summaryText, totalCountText, correctCountText, wrongCountText, accuracyText, mostWrongItemText, mostWrongCategoryText, topMistakeDetailText, content, closeButton);
    }

    public void Show(IReadOnlyList<WasteSessionSummary> history, System.Action closeAction)
    {
        _root.SetActive(true);
        BindButton(_closeButton, closeAction);
        DashboardStats stats = BuildStats(history);
        ApplyStats(stats);
        RebuildItems(history, stats);
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    private void ApplyStats(DashboardStats stats)
    {
        _titleText.text = "数据看板";
        _summaryText.text = "本地累计 " + stats.SessionCount + " 局    总正确率 " + FormatPercent(stats.OverallAccuracy) + "    最多错误物品 " + stats.TopWrongItemName;
        _totalCountText.text = stats.TotalProcessedCount.ToString();
        _correctCountText.text = stats.TotalCorrectCount.ToString();
        _wrongCountText.text = stats.TotalWrongCount.ToString();
        _accuracyText.text = FormatPercent(stats.OverallAccuracy);
        _mostWrongItemText.text = stats.TopWrongItemName + "\n" + stats.TopWrongItemCount + " 次错误";
        _mostWrongCategoryText.text = stats.TopWrongCategoryName + "\n" + stats.TopWrongCategoryCount + " 次错误";
        _topMistakeDetailText.text = stats.TopMistakeDetail;
    }

    private void RebuildItems(IReadOnlyList<WasteSessionSummary> history, DashboardStats stats)
    {
        for (int i = _itemsRoot.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(_itemsRoot.GetChild(i).gameObject);
        }

        if (history == null || history.Count <= 0)
        {
            WasteUiFactory.CreateText("Empty", _itemsRoot, "还没有累计到任何一局数据。", 30, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.82f, 0.84f, 0.86f, 1f));
            return;
        }

        AddItemRow("总分类数量", stats.TotalProcessedCount.ToString(), "记录所有已完成分类的总数。");
        AddItemRow("正确数量", stats.TotalCorrectCount.ToString(), "所有投放正确的数量。");
        AddItemRow("错误数量", stats.TotalWrongCount.ToString(), "所有投放错误的数量。");
        AddItemRow("正确率", FormatPercent(stats.OverallAccuracy), "正确数量 / 总分类数量。");
        AddItemRow("最多错误物品", stats.TopWrongItemName, stats.TopWrongItemCount + " 次错误");
        AddItemRow("最常错分类", stats.TopWrongCategoryName, stats.TopWrongCategoryCount + " 次错误");
        AddItemRow("最近一局", stats.LatestSessionTitle, stats.LatestSessionDetail);
    }

    private void AddItemRow(string title, string value, string detail)
    {
        Image row = WasteUiFactory.CreatePanel("ItemRow", _itemsRoot, new Color(0.11f, 0.14f, 0.16f, 1f));
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 112f;

        Text rowText = WasteUiFactory.CreateText("RowText", row.transform, string.Empty, 24, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        SetRect(rowText.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f), Vector2.zero, Vector2.one);
        rowText.text = title + "  " + value + "\n" + detail;
    }

    private static DashboardStats BuildStats(IReadOnlyList<WasteSessionSummary> history)
    {
        DashboardStats stats = new DashboardStats();
        if (history == null)
        {
            return stats;
        }

        Dictionary<string, int> itemMistakes = new Dictionary<string, int>();
        Dictionary<WasteCategory, int> categoryMistakes = new Dictionary<WasteCategory, int>();

        for (int i = 0; i < history.Count; i++)
        {
            WasteSessionSummary summary = history[i];
            if (summary == null)
            {
                continue;
            }

            stats.SessionCount++;
            stats.TotalCorrectCount += summary.CorrectCount;
            stats.TotalWrongCount += summary.WrongCount;
            stats.TotalProcessedCount += summary.TotalProcessedCount >= 0 ? summary.TotalProcessedCount : summary.CorrectCount + summary.WrongCount;
            stats.OverallAccuracyCorrect += summary.CorrectCount;
            stats.OverallAccuracyTotal += summary.CorrectCount + summary.WrongCount;

            if (i == history.Count - 1)
            {
                stats.LatestSessionTitle = string.IsNullOrWhiteSpace(summary.ModeName) ? "最近一局" : summary.ModeName;
                stats.LatestSessionDetail = "正确 " + summary.CorrectCount + "，错误 " + summary.WrongCount + "，正确率 " + FormatPercent(summary.Accuracy);
            }

            IReadOnlyList<ClassificationRecord> records = summary.Records;
            if (records == null)
            {
                continue;
            }

            for (int j = 0; j < records.Count; j++)
            {
                ClassificationRecord record = records[j];
                if (record == null || record.IsCorrect)
                {
                    continue;
                }

                string itemName = string.IsNullOrWhiteSpace(record.ItemName) ? "未知物品" : record.ItemName;
                itemMistakes.TryGetValue(itemName, out int itemCount);
                itemMistakes[itemName] = itemCount + 1;

                categoryMistakes.TryGetValue(record.CorrectCategory, out int categoryCount);
                categoryMistakes[record.CorrectCategory] = categoryCount + 1;
            }
        }

        stats.OverallAccuracy = stats.OverallAccuracyTotal <= 0 ? 0f : (float)stats.OverallAccuracyCorrect / stats.OverallAccuracyTotal;
        ResolveTopMistake(itemMistakes, out stats.TopWrongItemName, out stats.TopWrongItemCount);
        ResolveTopCategoryMistake(categoryMistakes, out stats.TopWrongCategoryName, out stats.TopWrongCategoryCount);
        stats.TopMistakeDetail = stats.TopWrongItemCount > 0
            ? "最多错误物品是 " + stats.TopWrongItemName + "，累计 " + stats.TopWrongItemCount + " 次分错。"
            : "目前没有错误记录。";
        return stats;
    }

    private static void ResolveTopMistake(Dictionary<string, int> counts, out string name, out int count)
    {
        name = "暂无";
        count = 0;
        if (counts == null)
        {
            return;
        }

        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (pair.Value <= count)
            {
                continue;
            }

            name = pair.Key;
            count = pair.Value;
        }
    }

    private static void ResolveTopCategoryMistake(Dictionary<WasteCategory, int> counts, out string name, out int count)
    {
        name = "暂无";
        count = 0;
        if (counts == null)
        {
            return;
        }

        foreach (KeyValuePair<WasteCategory, int> pair in counts)
        {
            if (pair.Value <= count)
            {
                continue;
            }

            name = WasteCategoryText.Format(pair.Key);
            count = pair.Value;
        }
    }

    private static void CreateMetricCard(Transform parent, string label, out Text valueText)
    {
        Image card = WasteUiFactory.CreatePanel("MetricCard", parent, new Color(0.11f, 0.14f, 0.16f, 1f));
        LayoutElement layoutElement = card.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 92f;

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 0f;
        layout.padding = new RectOffset(10, 10, 8, 8);

        Text labelText = WasteUiFactory.CreateText("Label", card.transform, label, 22, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.78f, 0.83f, 0.86f, 1f));
        LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 28f;

        valueText = WasteUiFactory.CreateText("Value", card.transform, "--", 46, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 42f;
    }

    private static Text CreateInfoBlock(Transform parent, string title)
    {
        Image card = WasteUiFactory.CreatePanel("InfoBlock", parent, new Color(0.11f, 0.14f, 0.16f, 1f));
        LayoutElement layoutElement = card.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 138f;

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 6f;
        layout.padding = new RectOffset(16, 16, 14, 14);

        Text titleText = WasteUiFactory.CreateText("Title", card.transform, title, 22, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.8f, 0.86f, 0.9f, 1f));
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 28f;

        Text valueText = WasteUiFactory.CreateText("Value", card.transform, "--", 28, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 80f;
        return valueText;
    }

    private static void BindButton(Button button, System.Action action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        if (action != null)
        {
            button.onClick.AddListener(() => action.Invoke());
        }
    }

    private static string FormatPercent(float ratio)
    {
        return (ratio * 100f).ToString("0.0") + "%";
    }

    private static void SetRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private sealed class DashboardStats
    {
        public int SessionCount;
        public int TotalProcessedCount;
        public int TotalCorrectCount;
        public int TotalWrongCount;
        public int OverallAccuracyCorrect;
        public int OverallAccuracyTotal;
        public float OverallAccuracy;
        public string TopWrongItemName = "暂无";
        public int TopWrongItemCount;
        public string TopWrongCategoryName = "暂无";
        public int TopWrongCategoryCount;
        public string TopMistakeDetail = "目前没有错误记录。";
        public string LatestSessionTitle = "最近一局";
        public string LatestSessionDetail = "暂无记录。";
    }
}
