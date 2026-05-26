using UnityEngine;
using UnityEngine.UI;

public sealed class WasteStartView
{
    private readonly GameObject _root;
    private readonly Button _stageProgressionButton;
    private readonly Button _startButton;
    private readonly Button _timedChallengeButton;

    private WasteStartView(
        GameObject root,
        Button stageProgressionButton,
        Button startButton,
        Button timedChallengeButton)
    {
        _root = root;
        _stageProgressionButton = stageProgressionButton;
        _startButton = startButton;
        _timedChallengeButton = timedChallengeButton;
    }

    public static WasteStartView Create(System.Action startAction)
    {
        return Create(startAction, null, null);
    }

    public static WasteStartView Create(
        System.Action startAction,
        System.Action timedChallengeAction,
        System.Action stageProgressionAction)
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("WasteStart");

        Image backdrop = WasteUiFactory.CreatePanel("Backdrop", root.transform, new Color(0.03f, 0.06f, 0.09f, 0.72f));
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image panel = WasteUiFactory.CreatePanel("Panel", root.transform, new Color(0.08f, 0.11f, 0.13f, 0.96f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(980f, 720f);

        Text titleText = WasteUiFactory.CreateText("Title", panelRect, "垃圾分类体验", 52, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
        SetStretch(titleText.rectTransform, new Vector2(48f, -98f), new Vector2(-48f, -24f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Text subtitleText = WasteUiFactory.CreateText(
            "Subtitle",
            panelRect,
            "推荐从标准闯关开始，先选择简单/中等/困难难度；也可开始游戏或体验限时挑战。",
            24,
            FontStyle.Normal,
            TextAnchor.UpperCenter,
            new Color(0.87f, 0.92f, 0.93f, 1f));
        SetStretch(subtitleText.rectTransform, new Vector2(64f, -164f), new Vector2(-64f, -100f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Image goalPanel = WasteUiFactory.CreatePanel("GoalPanel", panelRect, new Color(0.12f, 0.16f, 0.18f, 1f));
        SetStretch(goalPanel.rectTransform, new Vector2(54f, 282f), new Vector2(-54f, -208f), Vector2.zero, Vector2.one);

        Text goalText = WasteUiFactory.CreateText(
            "GoalText",
            goalPanel.transform,
            "体验目标\n• 观察垃圾外观并完成分类判断\n• 将垃圾投入对应的垃圾桶\n• 标准闯关可先选择难度，再完成对应挑战",
            28,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            Color.white);
        SetStretch(goalText.rectTransform, new Vector2(30f, -30f), new Vector2(-30f, -20f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Image controlsPanel = WasteUiFactory.CreatePanel("ControlsPanel", panelRect, new Color(0.09f, 0.13f, 0.11f, 1f));
        SetStretch(controlsPanel.rectTransform, new Vector2(54f, 146f), new Vector2(-54f, -388f), Vector2.zero, Vector2.one);

        Text controlsText = WasteUiFactory.CreateText(
            "ControlsText",
            controlsPanel.transform,
            "体验规则\n• 每件垃圾只会在松手时进行一次分类判定\n• 投放正确会得分，投放错误会扣分\n• 标准闯关需在选定难度的限时内完成目标正确投放数量",
            24,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            new Color(0.92f, 0.96f, 0.92f, 1f));
        SetStretch(controlsText.rectTransform, new Vector2(30f, -26f), new Vector2(-30f, -20f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Button stageProgressionButton = WasteUiFactory.CreateButton(
            "StageProgressionButton",
            panelRect,
            "标准闯关",
            new Color(0.15f, 0.57f, 0.36f, 1f),
            Color.white,
            stageProgressionAction);
        LayoutButton(stageProgressionButton, new Vector2(-320f, 48f), stageProgressionAction != null);

        Button startButton = WasteUiFactory.CreateButton(
            "StartButton",
            panelRect,
            "开始游戏",
            new Color(0.2f, 0.52f, 0.62f, 1f),
            Color.white,
            startAction);
        LayoutButton(startButton, new Vector2(0f, 48f), true);

        Button timedChallengeButton = WasteUiFactory.CreateButton(
            "TimedChallengeButton",
            panelRect,
            "限时挑战",
            new Color(0.17f, 0.44f, 0.94f, 1f),
            Color.white,
            timedChallengeAction);
        LayoutButton(timedChallengeButton, new Vector2(320f, 48f), timedChallengeAction != null);

        root.SetActive(false);
        return new WasteStartView(root, stageProgressionButton, startButton, timedChallengeButton);
    }

    public void Show(System.Action startAction)
    {
        Show(startAction, null, null);
    }

    public void Show(
        System.Action startAction,
        System.Action timedChallengeAction,
        System.Action stageProgressionAction)
    {
        _root.SetActive(true);

        BindButton(_startButton, startAction);
        BindButton(_timedChallengeButton, timedChallengeAction);
        BindButton(_stageProgressionButton, stageProgressionAction);

        bool hasStage = stageProgressionAction != null;
        bool hasTimed = timedChallengeAction != null;

        LayoutButton(_stageProgressionButton, new Vector2(hasTimed ? -320f : -170f, 48f), hasStage);
        LayoutButton(_startButton, new Vector2(hasStage && hasTimed ? 0f : (hasStage ? 170f : 0f), 48f), true);
        LayoutButton(_timedChallengeButton, new Vector2(hasStage ? 320f : 170f, 48f), hasTimed);
    }

    public void Hide()
    {
        _root.SetActive(false);
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

    private static void LayoutButton(Button button, Vector2 anchoredPosition, bool visible)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(visible);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(280f, 72f);
        rect.anchoredPosition = anchoredPosition;
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
