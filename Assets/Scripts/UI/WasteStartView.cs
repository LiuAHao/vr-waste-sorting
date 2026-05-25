using UnityEngine;
using UnityEngine.UI;

public sealed class WasteStartView
{
    private readonly GameObject _root;
    private readonly Button _startButton;
    private readonly Button _timedChallengeButton;

    private WasteStartView(GameObject root, Button startButton, Button timedChallengeButton)
    {
        _root = root;
        _startButton = startButton;
        _timedChallengeButton = timedChallengeButton;
    }

    public static WasteStartView Create(System.Action startAction)
    {
        return Create(startAction, null);
    }

    public static WasteStartView Create(System.Action startAction, System.Action timedChallengeAction)
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

        Text subtitleText = WasteUiFactory.CreateText("Subtitle", panelRect, "在限定时间内完成垃圾识别、抓取和正确投放。", 24, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.87f, 0.92f, 0.93f, 1f));
        SetStretch(subtitleText.rectTransform, new Vector2(64f, -164f), new Vector2(-64f, -100f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Image goalPanel = WasteUiFactory.CreatePanel("GoalPanel", panelRect, new Color(0.12f, 0.16f, 0.18f, 1f));
        SetStretch(goalPanel.rectTransform, new Vector2(54f, 282f), new Vector2(-54f, -208f), Vector2.zero, Vector2.one);

        Text goalText = WasteUiFactory.CreateText(
            "GoalText",
            goalPanel.transform,
            "本次体验目标\n• 观察垃圾外观并完成分类判断\n• 将垃圾投入对应的垃圾桶\n• 提高正确率并在规定时间内完成任务",
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
            "体验规则\n• 每件垃圾只会在松手时进行一次分类判定\n• 投放正确会得分，投放错误会扣分\n• 无论正误，垃圾都会消失并计入当前进度\n• 请在倒计时结束前尽可能完成全部分类任务",
            24,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            new Color(0.92f, 0.96f, 0.92f, 1f));
        SetStretch(controlsText.rectTransform, new Vector2(30f, -26f), new Vector2(-30f, -20f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Button startButton = WasteUiFactory.CreateButton("StartButton", panelRect, "开始游戏", new Color(0.15f, 0.57f, 0.36f, 1f), Color.white, startAction);
        RectTransform startButtonRect = startButton.GetComponent<RectTransform>();
        startButtonRect.anchorMin = new Vector2(0.5f, 0f);
        startButtonRect.anchorMax = new Vector2(0.5f, 0f);
        startButtonRect.sizeDelta = new Vector2(300f, 72f);
        startButtonRect.anchoredPosition = new Vector2(-170f, 48f);

        Button timedChallengeButton = WasteUiFactory.CreateButton(
            "TimedChallengeButton",
            panelRect,
            "限时挑战",
            new Color(0.17f, 0.44f, 0.94f, 1f),
            Color.white,
            timedChallengeAction);
        RectTransform timedButtonRect = timedChallengeButton.GetComponent<RectTransform>();
        timedButtonRect.anchorMin = new Vector2(0.5f, 0f);
        timedButtonRect.anchorMax = new Vector2(0.5f, 0f);
        timedButtonRect.sizeDelta = new Vector2(300f, 72f);
        timedButtonRect.anchoredPosition = new Vector2(170f, 48f);
        timedChallengeButton.gameObject.SetActive(timedChallengeAction != null);

        root.SetActive(false);
        return new WasteStartView(root, startButton, timedChallengeButton);
    }

    public void Show(System.Action startAction)
    {
        Show(startAction, null);
    }

    public void Show(System.Action startAction, System.Action timedChallengeAction)
    {
        _root.SetActive(true);
        _startButton.onClick.RemoveAllListeners();
        _startButton.onClick.AddListener(() => startAction?.Invoke());

        RectTransform startButtonRect = _startButton.GetComponent<RectTransform>();
        startButtonRect.anchoredPosition = new Vector2(timedChallengeAction != null ? -170f : 0f, 48f);

        if (_timedChallengeButton != null)
        {
            _timedChallengeButton.gameObject.SetActive(timedChallengeAction != null);
            _timedChallengeButton.onClick.RemoveAllListeners();
            if (timedChallengeAction != null)
            {
                _timedChallengeButton.onClick.AddListener(() => timedChallengeAction.Invoke());
            }
        }
    }

    public void Hide()
    {
        _root.SetActive(false);
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
