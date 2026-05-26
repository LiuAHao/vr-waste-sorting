using UnityEngine;
using UnityEngine.UI;

public sealed class WasteHudView
{
    private readonly GameObject _root;
    private readonly Text _timerText;
    private readonly Text _scoreText;
    private readonly Text _progressText;
    private readonly Image _feedbackPanel;
    private readonly Text _feedbackTitleText;
    private readonly Text _feedbackDetailText;

    private WasteHudView(GameObject root, Text timerText, Text scoreText, Text progressText, Image feedbackPanel, Text feedbackTitleText, Text feedbackDetailText)
    {
        _root = root;
        _timerText = timerText;
        _scoreText = scoreText;
        _progressText = progressText;
        _feedbackPanel = feedbackPanel;
        _feedbackTitleText = feedbackTitleText;
        _feedbackDetailText = feedbackDetailText;
    }

    public static WasteHudView Create()
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("WasteHUD");

        Image topBar = WasteUiFactory.CreatePanel("TopBar", root.transform, new Color(0.07f, 0.09f, 0.1f, 0.9f));
        RectTransform topRect = topBar.rectTransform;
        topRect.anchorMin = new Vector2(0.02f, 0.89f);
        topRect.anchorMax = new Vector2(0.98f, 0.985f);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;

        Text timerText = CreateStatCard(topRect, "TimerCard", "TimerText", "时间 00:00", new Vector2(0f, 0f), new Vector2(0.33f, 1f));
        Text scoreText = CreateStatCard(topRect, "ScoreCard", "ScoreText", "得分 0", new Vector2(0.33f, 0f), new Vector2(0.66f, 1f));
        Text progressText = CreateStatCard(topRect, "ProgressCard", "ProgressText", "进度 0/0", new Vector2(0.66f, 0f), new Vector2(1f, 1f));

        Image feedbackPanel = WasteUiFactory.CreatePanel("FeedbackPanel", root.transform, new Color(0.05f, 0.05f, 0.05f, 0.92f));
        RectTransform feedbackRect = feedbackPanel.rectTransform;
        feedbackRect.anchorMin = new Vector2(0.5f, 0.78f);
        feedbackRect.anchorMax = new Vector2(0.5f, 0.78f);
        feedbackRect.sizeDelta = new Vector2(760f, 110f);

        Text feedbackTitleText = WasteUiFactory.CreateText("FeedbackTitle", feedbackPanel.transform, string.Empty, 28, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
        RectTransform feedbackTitleRect = feedbackTitleText.rectTransform;
        feedbackTitleRect.anchorMin = new Vector2(0f, 0.48f);
        feedbackTitleRect.anchorMax = new Vector2(1f, 1f);
        feedbackTitleRect.offsetMin = new Vector2(18f, -4f);
        feedbackTitleRect.offsetMax = new Vector2(-18f, -8f);

        Text feedbackDetailText = WasteUiFactory.CreateText("FeedbackDetail", feedbackPanel.transform, string.Empty, 20, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.95f, 0.95f, 0.95f, 1f));
        RectTransform feedbackDetailRect = feedbackDetailText.rectTransform;
        feedbackDetailRect.anchorMin = new Vector2(0f, 0f);
        feedbackDetailRect.anchorMax = new Vector2(1f, 0.52f);
        feedbackDetailRect.offsetMin = new Vector2(20f, 8f);
        feedbackDetailRect.offsetMax = new Vector2(-20f, -2f);

        feedbackPanel.gameObject.SetActive(false);
        root.SetActive(false);
        return new WasteHudView(root, timerText, scoreText, progressText, feedbackPanel, feedbackTitleText, feedbackDetailText);
    }

    public void SetVisible(bool visible)
    {
        _root.SetActive(visible);
    }

    public void SetTimedChallengeStats(float remainingSeconds, int score, int processedCount)
    {
        _timerText.text = "时间 " + FormatTime(remainingSeconds);
        _scoreText.text = "得分 " + score;
        _progressText.text = "已处理 " + processedCount;
    }

    public void SetStageProgressionStats(
        string stageLabel,
        float remainingSeconds,
        int score,
        int stageCorrectCount,
        int stageTargetCount)
    {
        _timerText.text = "时间 " + FormatTime(remainingSeconds);
        _scoreText.text = "得分 " + score;
        _progressText.text = stageLabel + "  " + stageCorrectCount + "/" + stageTargetCount;
    }

    public void SetStats(float remainingSeconds, int score, int completed, int total)
    {
        _timerText.text = "时间 " + FormatTime(remainingSeconds);
        _scoreText.text = "得分 " + score;
        _progressText.text = "进度 " + completed + "/" + total;
    }

    public void ShowFeedback(bool isCorrect, string title, string detail)
    {
        _feedbackPanel.gameObject.SetActive(true);
        _feedbackTitleText.text = title;
        _feedbackTitleText.color = isCorrect ? new Color(0.36f, 0.93f, 0.55f, 1f) : new Color(1f, 0.42f, 0.42f, 1f);
        _feedbackDetailText.text = detail;
    }

    public void HideFeedback()
    {
        _feedbackPanel.gameObject.SetActive(false);
        _feedbackTitleText.text = string.Empty;
        _feedbackDetailText.text = string.Empty;
    }

    private static Text CreateStatCard(Transform parent, string cardName, string textName, string textValue, Vector2 anchorMin, Vector2 anchorMax)
    {
        Image card = WasteUiFactory.CreatePanel(cardName, parent, new Color(0.13f, 0.17f, 0.2f, 0.96f));
        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = anchorMin;
        cardRect.anchorMax = anchorMax;
        cardRect.offsetMin = new Vector2(10f, 8f);
        cardRect.offsetMax = new Vector2(-10f, -8f);

        Text label = WasteUiFactory.CreateText(textName, card.transform, textValue, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 6f);
        labelRect.offsetMax = new Vector2(-12f, -6f);
        return label;
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
