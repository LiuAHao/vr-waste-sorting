using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public static class WasteUiFactory
{
    private static Sprite _whiteSprite;
    private static Font _defaultFont;

    public static Font DefaultFont
    {
        get
        {
            if (_defaultFont != null)
            {
                return _defaultFont;
            }

            _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_defaultFont == null)
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0)
                {
                    _defaultFont = fonts[0];
                }
            }

            if (_defaultFont == null)
            {
                Debug.LogError("WasteUiFactory: 未找到可用的内置字体，UI 文本将无法正常显示。");
            }

            return _defaultFont;
        }
    }

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

    public static void HideLegacySceneUi()
    {
        Text[] texts = Object.FindObjectsOfType<Text>(true);
        var legacyParents = new HashSet<GameObject>();
        var legacyTexts = new List<GameObject>();

        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null || string.IsNullOrWhiteSpace(text.text))
            {
                continue;
            }

            if (!IsLegacyHudText(text.text))
            {
                continue;
            }

            legacyTexts.Add(text.gameObject);

            Transform parent = text.transform.parent;
            if (parent == null)
            {
                continue;
            }

            Text[] siblingTexts = parent.GetComponentsInChildren<Text>(true);
            bool hasCountdown = false;
            bool hasCleanup = false;
            for (int j = 0; j < siblingTexts.Length; j++)
            {
                string siblingText = siblingTexts[j] != null ? siblingTexts[j].text : string.Empty;
                hasCountdown |= siblingText.StartsWith("倒计时：");
                hasCleanup |= siblingText.StartsWith("清扫垃圾：");
            }

            if (hasCountdown && hasCleanup)
            {
                legacyParents.Add(parent.gameObject);
            }
        }

        foreach (GameObject parent in legacyParents)
        {
            if (parent != null)
            {
                parent.SetActive(false);
            }
        }

        for (int i = 0; i < legacyTexts.Count; i++)
        {
            GameObject legacyText = legacyTexts[i];
            if (legacyText != null && legacyText.activeSelf)
            {
                legacyText.SetActive(false);
            }
        }
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

    private static bool IsLegacyHudText(string text)
    {
        return text.StartsWith("倒计时：") || text.StartsWith("清扫垃圾：");
    }
}
