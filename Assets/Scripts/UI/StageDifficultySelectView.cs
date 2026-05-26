using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageDifficultySelectView
{
    private readonly GameObject _root;

    private StageDifficultySelectView(GameObject root)
    {
        _root = root;
    }

    public static StageDifficultySelectView Create()
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("StageDifficultySelect");

        Image backdrop = WasteUiFactory.CreatePanel("Backdrop", root.transform, new Color(0.03f, 0.06f, 0.09f, 0.78f));
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        root.SetActive(false);
        return new StageDifficultySelectView(root);
    }

    public void Show(StageProgressionConfig config, Action<int> onSelectDifficulty, Action onBack)
    {
        if (config == null)
        {
            return;
        }

        ClearContent(_root.transform);

        Image panel = WasteUiFactory.CreatePanel("Panel", _root.transform, new Color(0.08f, 0.11f, 0.13f, 0.96f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 680f);

        Text titleText = WasteUiFactory.CreateText(
            "Title",
            panelRect,
            "选择闯关难度",
            46,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            Color.white);
        SetStretch(titleText.rectTransform, new Vector2(40f, -88f), new Vector2(-40f, -20f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        RectTransform listRect = WasteUiFactory.CreateRect("DifficultyList", panelRect);
        SetStretch(listRect, new Vector2(56f, 120f), new Vector2(-56f, -96f), Vector2.zero, Vector2.one);

        VerticalLayoutGroup layout = listRect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = listRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        int stageCount = config.StageCount;
        for (int i = 0; i < stageCount; i++)
        {
            StageDefinition stage = config.GetStage(i);
            if (stage == null)
            {
                continue;
            }

            int capturedIndex = i;
            CreateDifficultyButton(
                listRect,
                stage.stageName,
                GetDifficultyColor(i),
                () => onSelectDifficulty?.Invoke(capturedIndex));
        }

        Button backButton = WasteUiFactory.CreateButton(
            "BackButton",
            panelRect,
            "返回",
            new Color(0.28f, 0.32f, 0.36f, 1f),
            Color.white,
            onBack);
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.sizeDelta = new Vector2(220f, 58f);
        backRect.anchoredPosition = new Vector2(0f, 36f);

        _root.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    private static void CreateDifficultyButton(
        Transform parent,
        string title,
        Color backgroundColor,
        Action onClick)
    {
        Image row = WasteUiFactory.CreatePanel("DifficultyRow", parent, new Color(0.11f, 0.15f, 0.17f, 1f));
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 88f;

        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row;
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = backgroundColor * 1.08f;
        colors.pressedColor = backgroundColor * 0.9f;
        colors.selectedColor = backgroundColor;
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        Text titleText = WasteUiFactory.CreateText(
            "Title",
            row.transform,
            title,
            32,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white);
        SetStretch(titleText.rectTransform, new Vector2(24f, 12f), new Vector2(-24f, -12f), Vector2.zero, Vector2.one);
    }

    private static Color GetDifficultyColor(int index)
    {
        switch (index)
        {
            case 0:
                return new Color(0.18f, 0.55f, 0.38f, 1f);
            case 1:
                return new Color(0.2f, 0.48f, 0.72f, 1f);
            default:
                return new Color(0.62f, 0.32f, 0.24f, 1f);
        }
    }

    private static void ClearContent(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
        }
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
