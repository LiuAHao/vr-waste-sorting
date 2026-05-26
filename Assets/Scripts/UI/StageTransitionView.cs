using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageTransitionView
{
    private readonly GameObject _root;
    private readonly Text _titleText;
    private readonly Text _summaryText;
    private readonly Text _nextStageText;

    private StageTransitionView(GameObject root, Text titleText, Text summaryText, Text nextStageText)
    {
        _root = root;
        _titleText = titleText;
        _summaryText = summaryText;
        _nextStageText = nextStageText;
    }

    public static StageTransitionView Create()
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("StageTransition");

        Image backdrop = WasteUiFactory.CreatePanel("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.55f));
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image panel = WasteUiFactory.CreatePanel("Panel", backdrop.transform, new Color(0.08f, 0.11f, 0.13f, 0.96f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(860f, 320f);

        Text titleText = WasteUiFactory.CreateText("Title", panel.transform, string.Empty, 34, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
        SetRect(titleText.rectTransform, new Vector2(24f, -24f), new Vector2(-24f, -72f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Text summaryText = WasteUiFactory.CreateText("Summary", panel.transform, string.Empty, 24, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.88f, 0.93f, 0.94f, 1f));
        SetRect(summaryText.rectTransform, new Vector2(32f, -88f), new Vector2(-32f, -150f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Text nextStageText = WasteUiFactory.CreateText("NextStage", panel.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.UpperCenter, new Color(0.55f, 0.9f, 0.7f, 1f));
        SetRect(nextStageText.rectTransform, new Vector2(32f, -156f), new Vector2(-32f, -24f), new Vector2(0f, 0f), new Vector2(1f, 1f));

        root.SetActive(false);
        return new StageTransitionView(root, titleText, summaryText, nextStageText);
    }

    public void Show(
        string completedStageName,
        string stageSummary,
        string nextStageName,
        string nextStagePreview,
        int nextStageNumber,
        int totalStageCount)
    {
        _root.SetActive(true);
        _titleText.text = "本关完成 · " + completedStageName;
        _summaryText.text = stageSummary;
        _nextStageText.text = "即将进入第 " + nextStageNumber + "/" + totalStageCount + " 关："
            + nextStageName + "\n" + nextStagePreview;
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    private static void SetRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
