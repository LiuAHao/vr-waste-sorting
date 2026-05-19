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

        Image topBar = WasteUiFactory.CreatePanel("TopBar", root.transform, new Color(0.08f, 0.09f, 0.1f, 0.85f));
        RectTransform topRect = topBar.rectTransform;
        topRect.anchorMin = new Vector2(0.04f, 0.9f);
        topRect.anchorMax = new Vector2(0.96f, 0.985f);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;

        Text timerText = WasteUiFactory.CreateText("TimerText", topRect, "时间 00:00", 28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        SetAnchors(timerText.rectTransform, new Vector2(0f, 0f), new Vector2(0.33f, 1f), new Vector2(24f, 0f), new Vector2(-12f, 0f));

        Text scoreText = WasteUiFactory.CreateText("ScoreText", topRect, "得分 0", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(scoreText.rectTransform, new Vector2(0.33f, 0f), new Vector2(0.66f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f));

        Text progressText = WasteUiFactory.CreateText("ProgressText", topRect, "进度 0/0", 28, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
        SetAnchors(progressText.rectTransform, new Vector2(0.66f, 0f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-24f, 0f));

        Image feedbackPanel = WasteUiFactory.CreatePanel("FeedbackPanel", root.transform, new Color(0.05f, 0.05f, 0.05f, 0.88f));
        RectTransform feedbackRect = feedbackPanel.rectTransform;
        feedbackRect.anchorMin = new Vector2(0.5f, 0.82f);
        feedbackRect.anchorMax = new Vector2(0.5f, 0.82f);
        feedbackRect.offsetMin = new Vector2(-380f, -10f);
        feedbackRect.offsetMax = new Vector2(380f, 78f);

        Text feedbackTitleText = WasteUiFactory.CreateText("FeedbackTitle", feedbackPanel.transform, string.Empty, 26, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
        SetAnchors(feedbackTitleText.rectTransform, new Vector2(0f, 0.4f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(-18f, -6f));

        Text feedbackDetailText = WasteUiFactory.CreateText("FeedbackDetail", feedbackPanel.transform, string.Empty, 20, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.95f, 0.95f, 0.95f, 1f));
        SetAnchors(feedbackDetailText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(18f, 6f), new Vector2(-18f, -2f));

        feedbackPanel.gameObject.SetActive(false);
        root.SetActive(false);

        return new WasteHudView(root, timerText, scoreText, progressText, feedbackPanel, feedbackTitleText, feedbackDetailText);
    }

    public void SetVisible(bool visible)
    {
        _root.SetActive(visible);
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

    private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
