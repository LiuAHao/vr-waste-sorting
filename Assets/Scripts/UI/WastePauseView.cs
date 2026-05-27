using UnityEngine;
using UnityEngine.UI;

public sealed class WastePauseView
{
    private readonly GameObject _root;
    private readonly Button _resumeButton;
    private readonly Button _backToMenuButton;

    private WastePauseView(GameObject root, Button resumeButton, Button backToMenuButton)
    {
        _root = root;
        _resumeButton = resumeButton;
        _backToMenuButton = backToMenuButton;
    }

    public static WastePauseView Create()
    {
        GameObject root = WasteUiFactory.CreateCanvasRoot("WastePause");

        Image backdrop = WasteUiFactory.CreatePanel("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.62f));
        RectTransform backdropRect = backdrop.rectTransform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image panel = WasteUiFactory.CreatePanel("Panel", backdrop.transform, new Color(0.08f, 0.11f, 0.13f, 0.97f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 280f);

        Text titleText = WasteUiFactory.CreateText("Title", panel.transform, "已暂停", 42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetRect(titleText.rectTransform, new Vector2(24f, -82f), new Vector2(-24f, -22f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Text detailText = WasteUiFactory.CreateText("Detail", panel.transform, "按 Esc 可继续游戏，或直接返回主菜单。", 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.88f, 0.93f, 0.94f, 1f));
        SetRect(detailText.rectTransform, new Vector2(28f, -138f), new Vector2(-28f, -92f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        Button resumeButton = WasteUiFactory.CreateButton("ResumeButton", panel.transform, "返回游戏", new Color(0.2f, 0.52f, 0.62f, 1f), Color.white, null);
        RectTransform resumeRect = resumeButton.GetComponent<RectTransform>();
        resumeRect.anchorMin = new Vector2(0.5f, 0f);
        resumeRect.anchorMax = new Vector2(0.5f, 0f);
        resumeRect.sizeDelta = new Vector2(240f, 64f);
        resumeRect.anchoredPosition = new Vector2(-138f, 34f);

        Button backToMenuButton = WasteUiFactory.CreateButton("BackToMenuButton", panel.transform, "返回主菜单", new Color(0.27f, 0.31f, 0.37f, 1f), Color.white, null);
        RectTransform backRect = backToMenuButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.sizeDelta = new Vector2(240f, 64f);
        backRect.anchoredPosition = new Vector2(138f, 34f);

        root.SetActive(false);
        return new WastePauseView(root, resumeButton, backToMenuButton);
    }

    public void Show(System.Action resumeAction, System.Action backToMenuAction)
    {
        _root.SetActive(true);
        BindButton(_resumeButton, resumeAction);
        BindButton(_backToMenuButton, backToMenuAction);
    }

    public void Hide()
    {
        _root.SetActive(false);
        BindButton(_resumeButton, null);
        BindButton(_backToMenuButton, null);
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

    private static void SetRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
