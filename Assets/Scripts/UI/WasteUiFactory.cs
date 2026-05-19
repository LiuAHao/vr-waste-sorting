using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WasteUiFactory
{
    private static Sprite _whiteSprite;

    public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static GameObject EnsureEventSystem()
    {
        EventSystem current = EventSystem.current;
        if (current != null)
        {
            return current.gameObject;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        return eventSystem;
    }

    public static GameObject CreateCanvasRoot(string name)
    {
        GameObject root = new GameObject(name);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();
        Object.DontDestroyOnLoad(root);
        return root;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    public static Image CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = WhiteSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    public static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Text uiText = rect.gameObject.AddComponent<Text>();
        uiText.font = DefaultFont;
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.fontStyle = style;
        uiText.alignment = anchor;
        uiText.color = color;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        return uiText;
    }

    public static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, System.Action onClick)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = WhiteSprite;
        image.color = backgroundColor;

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = backgroundColor * 1.1f;
        colors.selectedColor = backgroundColor * 0.95f;
        colors.pressedColor = backgroundColor * 0.85f;
        button.colors = colors;

        Text text = CreateText("Label", rect, label, 26, FontStyle.Bold, TextAnchor.MiddleCenter, textColor);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(() => onClick?.Invoke());
        return button;
    }

    private static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite == null)
            {
                _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            }

            return _whiteSprite;
        }
    }
}
