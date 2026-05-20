using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class WasteResultView
{
    private readonly GameObject _root;
    private readonly Text _titleText;
    private readonly Text _summaryText;
    private readonly Text _accuracyText;
    private readonly Text _timeText;
    private readonly Text _scoreText;
    private readonly Text _impactText;
    private readonly Transform _mistakeContent;
    private readonly Button _restartButton;

    private WasteResultView(
        GameObject root,
        Text titleText,
        Text summaryText,
        Text accuracyText,
        Text timeText,
        Text scoreText,
        Text impactText,
        Transform mistakeContent,
        Button restartButton)
    {
        _root = root;
        _titleText = titleText;
        _summaryText = summaryText;
        _accuracyText = accuracyText;
        _timeText = timeText;
        _scoreText = scoreText;
        _impactText = impactText;
        _mistakeContent = mistakeContent;
        _restartButton = restartButton;
    }

    public static WasteResultView Create(System.Action restartAction)
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("WasteResult");

        Image backdrop = WasteUiFactory.CreatePanel("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.68f));
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image panel = WasteUiFactory.CreatePanel("Panel", backdrop.transform, new Color(0.08f, 0.1f, 0.12f, 0.96f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1120f, 720f);

        Text titleText = WasteUiFactory.CreateText("Title", panel.transform, "分类完成", 42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -74f), new Vector2(-36f, -18f));

        Text summaryText = WasteUiFactory.CreateText("Summary", panel.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        SetRect(summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(42f, -126f), new Vector2(-42f, -86f));

        RectTransform leftColumn = WasteUiFactory.CreateRect("LeftColumn", panel.transform);
        SetRect(leftColumn, new Vector2(0f, 0f), new Vector2(0.54f, 1f), new Vector2(38f, 96f), new Vector2(-18f, -150f));

        RectTransform rightColumn = WasteUiFactory.CreateRect("RightColumn", panel.transform);
        SetRect(rightColumn, new Vector2(0.54f, 0f), new Vector2(1f, 1f), new Vector2(18f, 96f), new Vector2(-38f, -150f));

        Text accuracyText = CreateMetricCard(leftColumn, "正确率", new Vector2(0f, 0.73f), new Vector2(1f, 1f));
        Text timeText = CreateMetricCard(leftColumn, "用时", new Vector2(0f, 0.46f), new Vector2(1f, 0.71f));
        Text scoreText = CreateMetricCard(leftColumn, "得分", new Vector2(0f, 0.19f), new Vector2(1f, 0.44f));

        Image impactPanel = WasteUiFactory.CreatePanel("ImpactPanel", leftColumn, new Color(0.11f, 0.14f, 0.16f, 1f));
        SetRect(impactPanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.16f), Vector2.zero, Vector2.zero);
        Text impactText = WasteUiFactory.CreateText("Impact", impactPanel.transform, string.Empty, 20, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.84f, 0.94f, 0.88f, 1f));
        SetRect(impactText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 16f), new Vector2(-18f, -16f));

        Text mistakesTitle = WasteUiFactory.CreateText("MistakesTitle", rightColumn, "错误记录", 26, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        SetRect(mistakesTitle.rectTransform, new Vector2(0f, 0.9f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -8f));

        Image listPanel = WasteUiFactory.CreatePanel("MistakePanel", rightColumn, new Color(0.04f, 0.05f, 0.06f, 0.95f));
        SetRect(listPanel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);

        Image viewport = WasteUiFactory.CreatePanel("Viewport", listPanel.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform viewportRect = viewport.rectTransform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(16f, 16f);
        viewportRect.offsetMax = new Vector2(-16f, -16f);
        viewport.gameObject.AddComponent<RectMask2D>();

        ScrollRect scrollRect = listPanel.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        RectTransform contentRect = WasteUiFactory.CreateRect("Content", viewport.transform);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        scrollRect.content = contentRect;

        VerticalLayoutGroup contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.spacing = 10f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter contentFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button restartButton = WasteUiFactory.CreateButton("RestartButton", panel.transform, "重新开始", new Color(0.17f, 0.44f, 0.94f, 1f), Color.white, restartAction);
        RectTransform restartRect = restartButton.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0.5f, 0f);
        restartRect.anchorMax = new Vector2(0.5f, 0f);
        restartRect.sizeDelta = new Vector2(260f, 58f);
        restartRect.anchoredPosition = new Vector2(0f, 34f);

        root.SetActive(false);
        return new WasteResultView(root, titleText, summaryText, accuracyText, timeText, scoreText, impactText, contentRect, restartButton);
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    public void Show(WasteSessionSummary summary, System.Action restartAction)
    {
        _root.SetActive(true);
        _restartButton.onClick.RemoveAllListeners();
        _restartButton.onClick.AddListener(() => restartAction?.Invoke());

        _titleText.text = summary.CorrectCount >= summary.TotalTargets ? "分类完成" : "时间到";
        _summaryText.text = $"正确 {summary.CorrectCount}/{summary.TotalTargets}    错误 {summary.WrongCount}";
        _accuracyText.text = FormatPercent(summary.Accuracy);
        _timeText.text = FormatTime(summary.ElapsedSeconds) + " / " + FormatTime(summary.TimeLimitSeconds);
        _scoreText.text = summary.Score.ToString();
        _impactText.text = BuildImpactMessage(summary);

        RebuildMistakeList(summary.Records);
    }

    private void RebuildMistakeList(IReadOnlyList<ClassificationRecord> records)
    {
        for (int i = _mistakeContent.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(_mistakeContent.GetChild(i).gameObject);
        }

        bool hasMistakes = false;
        for (int i = 0; i < records.Count; i++)
        {
            ClassificationRecord record = records[i];
            if (record.IsCorrect)
            {
                continue;
            }

            hasMistakes = true;
            Image rowPanel = WasteUiFactory.CreatePanel("MistakeRow", _mistakeContent, new Color(0.11f, 0.12f, 0.14f, 0.95f));
            LayoutElement rowLayout = rowPanel.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 78f;

            Text rowText = WasteUiFactory.CreateText("RowText", rowPanel.transform, FormatMistake(record), 18, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.96f, 0.96f, 0.96f, 1f));
            SetRect(rowText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 10f), new Vector2(-16f, -10f));
        }

        if (!hasMistakes)
        {
            Text row = WasteUiFactory.CreateText("MistakeRow", _mistakeContent, "本轮没有错误记录。", 22, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.82f, 0.82f, 0.82f, 0.9f));
            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 56f;
        }
    }

    private static Text CreateMetricCard(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        Image card = WasteUiFactory.CreatePanel("MetricCard", parent, new Color(0.11f, 0.14f, 0.16f, 1f));
        SetRect(card.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Text labelText = WasteUiFactory.CreateText("Label", card.transform, label, 18, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.78f, 0.83f, 0.86f, 1f));
        SetRect(labelText.rectTransform, new Vector2(0f, 0.56f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, -4f));

        Text valueText = WasteUiFactory.CreateText("Value", card.transform, "--", 30, FontStyle.Bold, TextAnchor.LowerCenter, Color.white);
        SetRect(valueText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.62f), new Vector2(10f, 10f), new Vector2(-10f, 0f));
        return valueText;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static string FormatMistake(ClassificationRecord record)
    {
        string reason = string.IsNullOrWhiteSpace(record.Reason) ? "未提供原因" : record.Reason;
        return $"{record.ItemName} 误投到 {record.SelectedBinName}\n正确应为 {WasteCategoryText.Format(record.CorrectCategory)}，原因：{reason}";
    }

    private static string BuildImpactMessage(WasteSessionSummary summary)
    {
        if (summary.TotalTargets <= 0)
        {
            return "本轮没有可统计的分类目标。";
        }

        if (summary.Accuracy >= 0.85f)
        {
            return "本轮分类表现很好，已经能比较稳定地完成常见垃圾分类。";
        }

        if (summary.WrongCount > summary.CorrectCount)
        {
            return "本轮仍有不少容易混淆的物品，建议根据错误记录再练一轮。";
        }

        return "本轮大部分分类已经完成，继续熟悉易错物品就能进一步提高正确率。";
    }

    private static string FormatPercent(float ratio)
    {
        return (ratio * 100f).ToString("0.0") + "%";
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
