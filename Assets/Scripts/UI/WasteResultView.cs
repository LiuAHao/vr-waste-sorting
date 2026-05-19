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
    private readonly Transform _mistakeContent;
    private readonly Button _restartButton;

    private WasteResultView(GameObject root, Text titleText, Text summaryText, Text accuracyText, Text timeText, Text scoreText, Transform mistakeContent, Button restartButton)
    {
        _root = root;
        _titleText = titleText;
        _summaryText = summaryText;
        _accuracyText = accuracyText;
        _timeText = timeText;
        _scoreText = scoreText;
        _mistakeContent = mistakeContent;
        _restartButton = restartButton;
    }

    public static WasteResultView Create(System.Action restartAction)
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("WasteResult");

        Image backdrop = WasteUiFactory.CreatePanel("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.6f));
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image panel = WasteUiFactory.CreatePanel("Panel", backdrop.transform, new Color(0.1f, 0.11f, 0.13f, 0.96f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.offsetMin = new Vector2(-540f, -320f);
        panelRect.offsetMax = new Vector2(540f, 320f);

        Text titleText = WasteUiFactory.CreateText("Title", panelRect, "分类完成", 38, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
        SetAnchors(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -72f), new Vector2(-36f, -12f));

        Text summaryText = WasteUiFactory.CreateText("Summary", panelRect, string.Empty, 24, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.93f, 0.93f, 0.93f, 1f));
        SetAnchors(summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -132f), new Vector2(-36f, -96f));

        Text accuracyText = WasteUiFactory.CreateText("Accuracy", panelRect, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        SetAnchors(accuracyText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(36f, -180f), new Vector2(-12f, -144f));

        Text timeText = WasteUiFactory.CreateText("Time", panelRect, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperRight, Color.white);
        SetAnchors(timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(12f, -180f), new Vector2(-36f, -144f));

        Text scoreText = WasteUiFactory.CreateText("Score", panelRect, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.92f, 0.94f, 1f, 1f));
        SetAnchors(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -228f), new Vector2(-36f, -192f));

        Image listPanel = WasteUiFactory.CreatePanel("MistakePanel", panelRect, new Color(0.06f, 0.07f, 0.08f, 0.95f));
        RectTransform listRect = listPanel.rectTransform;
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.offsetMin = new Vector2(36f, 84f);
        listRect.offsetMax = new Vector2(-36f, -264f);

        Image viewport = WasteUiFactory.CreatePanel("Viewport", listPanel.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform viewportRect = viewport.rectTransform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        ScrollRect scrollRect = listPanel.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        Image content = WasteUiFactory.CreatePanel("Content", viewport.transform, new Color(0f, 0f, 0f, 0f));
        RectTransform contentRect = content.rectTransform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(12f, 12f);
        contentRect.offsetMax = new Vector2(-12f, -12f);
        scrollRect.content = contentRect;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 10f;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button restartButton = WasteUiFactory.CreateButton("RestartButton", panelRect, "重开", new Color(0.2f, 0.52f, 0.96f, 1f), Color.white, restartAction);
        RectTransform buttonRect = restartButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(220f, 52f);
        buttonRect.anchoredPosition = new Vector2(0f, 26f);

        root.SetActive(false);
        return new WasteResultView(root, titleText, summaryText, accuracyText, timeText, scoreText, content.transform, restartButton);
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
        _summaryText.text = "正确 " + summary.CorrectCount + "/" + summary.TotalTargets + "  错误 " + summary.WrongCount;
        _accuracyText.text = "正确率 " + FormatPercent(summary.Accuracy);
        _timeText.text = "用时 " + FormatTime(summary.ElapsedSeconds) + " / " + FormatTime(summary.TimeLimitSeconds);
        _scoreText.text = "得分 " + summary.Score;

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
            Text row = WasteUiFactory.CreateText("MistakeRow", _mistakeContent, FormatMistake(record), 20, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.95f, 1f));
            row.rectTransform.sizeDelta = new Vector2(0f, 56f);
        }

        if (!hasMistakes)
        {
            Text row = WasteUiFactory.CreateText("MistakeRow", _mistakeContent, "没有错误记录", 22, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.82f, 0.82f, 0.82f, 0.85f));
            row.rectTransform.sizeDelta = new Vector2(0f, 48f);
        }
    }

    private static string FormatMistake(ClassificationRecord record)
    {
        string reason = string.IsNullOrWhiteSpace(record.Reason) ? "未提供原因" : record.Reason;
        return record.ItemName + " | 误投到 " + record.SelectedBinName + " | 正确应为 " + FormatCategory(record.CorrectCategory) + " | " + reason;
    }

    private static string FormatCategory(WasteCategory category)
    {
        switch (category)
        {
            case WasteCategory.Recyclable:
                return "可回收物";
            case WasteCategory.Hazardous:
                return "有害垃圾";
            case WasteCategory.Kitchen:
                return "厨余垃圾";
            default:
                return "其他垃圾";
        }
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

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
