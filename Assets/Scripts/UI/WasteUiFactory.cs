using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WasteUiFactory
{
    private static Sprite _whiteSprite;
    private static Font _defaultFont;
    private static readonly string[] PreferredChineseFonts =
    {
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "PingFang SC",
        "Hiragino Sans GB",
        "Noto Sans CJK SC",
        "Source Han Sans SC",
        "SimHei",
        "SimSun"
    };

    public static Font DefaultFont
    {
        get
        {
            if (_defaultFont != null)
            {
                return _defaultFont;
            }

            _defaultFont = TryCreateChineseOsFont();

            if (_defaultFont == null)
            {
                _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

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
                Debug.LogError("WasteUiFactory: 未找到可用的内置字体，UI 文本可能无法正常显示。");
            }

            return _defaultFont;
        }
    }

    public static GameObject EnsureEventSystem()
    {
        EventSystem current = EventSystem.current;
        if (current != null)
        {
            // VR 模式下：确保现有 EventSystem 有 XRUIInputModule，移除冲突的 StandaloneInputModule
            if (IsXRActive())
            {
                EnsureXRUIInputModule(current.gameObject);
            }
            return current.gameObject;
        }

        // 没有 EventSystem 时才创建（正常情况下 XR Interaction Setup Prefab 已自带）
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();

        if (IsXRActive())
        {
            EnsureXRUIInputModule(eventSystem);
        }
        else
        {
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        return eventSystem;
    }

    private static void EnsureXRUIInputModule(GameObject eventSystemGo)
    {
        // 移除 StandaloneInputModule（与 XRUIInputModule 冲突）
        StandaloneInputModule standalone = eventSystemGo.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Object.Destroy(standalone);
        }

        // 用反射添加 XRUIInputModule，避免 XRI 未导入时编译报错
        const string xrUIInputModuleType =
            "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule," +
            "Unity.XR.Interaction.Toolkit";
        System.Type t = System.Type.GetType(xrUIInputModuleType);
        if (t != null && eventSystemGo.GetComponent(t) == null)
        {
            eventSystemGo.AddComponent(t);
        }
    }

    private static bool IsXRActive()
    {
        return UnityEngine.XR.XRSettings.isDeviceActive
            || UnityEngine.XR.XRSettings.loadedDeviceName.Length > 0;
    }

    public static GameObject CreateCanvasRoot(string name)
    {
        GameObject root = new GameObject(name);
        Canvas canvas = root.AddComponent<Canvas>();

        bool isVR = UnityEngine.XR.XRSettings.isDeviceActive
                 || UnityEngine.XR.XRSettings.loadedDeviceName.Length > 0;

        if (isVR)
        {
            // VR 模式：World Space Canvas，放在玩家正前方
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            // 设置 Canvas RectTransform 的参考尺寸（1920x1080），
            // 让使用相对锚点的 UI 元素（如 HUD TopBar）能正确计算高度
            RectTransform canvasRect = root.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);

            // 缩放：原 1920x1080 UI 映射到约 1.92m x 1.08m 的世界空间面板
            root.transform.localScale = Vector3.one * 0.001f;

            // 初始位置：摄像机正前方 2.5m，视线中央（轻微下偏）
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 forward = cam.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                forward.Normalize();
                root.transform.position = cam.transform.position + forward * 2.5f + Vector3.up * (-0.1f);
                root.transform.rotation = Quaternion.LookRotation(forward);
            }

            // World Space 下用固定尺寸
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            // 添加 TrackedDeviceGraphicRaycaster 支持手柄射线点击（用反射，避免 XRI 未导入时报错）
            const string trackedRaycasterType =
                "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster," +
                "Unity.XR.Interaction.Toolkit";
            System.Type t = System.Type.GetType(trackedRaycasterType);
            if (t != null)
            {
                Component raycaster = root.AddComponent(t);
                // 设置 maxRaycastDistance 为一个较大的值，确保能射到 Canvas
                System.Reflection.PropertyInfo maxDistProp = t.GetProperty("maxRaycastDistance");
                if (maxDistProp != null && maxDistProp.CanWrite)
                {
                    maxDistProp.SetValue(raycaster, 10f);
                }
            }
            else
            {
                // XRI 未导入时退回普通 GraphicRaycaster
                root.AddComponent<GraphicRaycaster>();
            }

            // 确保 Canvas 在 Default Layer（Layer 0），让 XR Ray Interactor 能检测到
            root.layer = 0;
            SetLayerRecursively(root.transform, 0);

            // 让 Canvas 跟随摄像机，确保玩家移动/转头后 UI 始终可见
            VRCanvasFollower follower = root.AddComponent<VRCanvasFollower>();
            follower.distance = 2.5f;
            follower.verticalOffset = -0.1f;
            follower.followAngleThreshold = 30f;
            follower.followSpeed = 2f;
        }
        else
        {
            // 桌面模式：保持原有 Screen Space Overlay
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
        }

        Object.DontDestroyOnLoad(root);
        return root;
    }

    /// <summary>递归设置 GameObject 及其所有子物体的 Layer</summary>
    private static void SetLayerRecursively(Transform trans, int layer)
    {
        trans.gameObject.layer = layer;
        for (int i = 0; i < trans.childCount; i++)
        {
            SetLayerRecursively(trans.GetChild(i), layer);
        }
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

            if (BelongsToRuntimeUi(text.transform))
            {
                continue;
            }

            if (!IsLegacyHudText(text.text))
            {
                continue;
            }

            legacyTexts.Add(text.gameObject);

            Transform parent = text.transform.parent;
            if (parent == null || BelongsToRuntimeUi(parent))
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

        Text buttonText = CreateText("Label", rect, label, 26, FontStyle.Bold, TextAnchor.MiddleCenter, textColor);
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(() => onClick?.Invoke());
        return button;
    }

    private static bool BelongsToRuntimeUi(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        Transform current = target;
        while (current != null)
        {
            string name = current.name;
            if (name == "WasteHUD" || name == "WasteResult" || name == "WasteStart")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
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
        return text.StartsWith("倒计时：")
            || text.StartsWith("清扫垃圾：");
    }

    private static Font TryCreateChineseOsFont()
    {
        for (int i = 0; i < PreferredChineseFonts.Length; i++)
        {
            string fontName = PreferredChineseFonts[i];
            if (string.IsNullOrWhiteSpace(fontName))
            {
                continue;
            }

            try
            {
                Font font = Font.CreateDynamicFontFromOSFont(fontName, 32);
                if (font != null)
                {
                    return font;
                }
            }
            catch
            {
                // 某些平台或编辑器环境下字体不存在会抛异常，继续尝试下一个候选字体。
            }
        }

        return null;
    }
}
