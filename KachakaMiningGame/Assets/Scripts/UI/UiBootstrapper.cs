using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UiBootstrapper
{
    public static void EnsureCanvas(
        out ScoreView scoreView,
        out TimerView timerView,
        out GameStateView stateView,
        out PopupController popupController,
        out StartScreenView startScreenView,
        out FinishScreenView finishScreenView,
        out PauseMenuView pauseMenuView
    )
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (canvas.GetComponent<CanvasScaler>() == null)
        {
            canvas.gameObject.AddComponent<CanvasScaler>();
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        Transform canvasTransform = canvas.transform;
        scoreView = EnsureTmpTextObject<ScoreView>(
            canvasTransform,
            "ScoreText",
            new Vector2(24f, -24f),
            TextAlignmentOptions.TopLeft
        );
        timerView = EnsureTmpTextObject<TimerView>(
            canvasTransform,
            "TimerText",
            new Vector2(-24f, -24f),
            TextAlignmentOptions.TopRight
        );
        stateView = EnsureTextObject<GameStateView>(
            canvasTransform,
            "StateText",
            new Vector2(24f, -64f),
            TextAnchor.UpperLeft
        );
        popupController = EnsurePopup(canvasTransform);
        startScreenView = EnsureStartScreen(canvasTransform);
        finishScreenView = EnsureFinishScreen(canvasTransform);
        pauseMenuView = EnsurePauseMenu(canvasTransform);
    }

    private static T EnsureTmpTextObject<T>(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        TextAlignmentOptions alignment
    )
        where T : Component
    {
        Transform existing = parent.Find(objectName);
        bool createdObject = existing == null;
        GameObject textObject = createdObject ? new GameObject(objectName) : existing.gameObject;
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = alignment;
        }

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        if (createdObject)
        {
            bool alignRight = alignment == TextAlignmentOptions.TopRight;
            rectTransform.anchorMin = alignRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.pivot = alignRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(260f, 42f);
        }

        T component = textObject.GetComponent<T>();
        return component != null ? component : textObject.AddComponent<T>();
    }

    private static T EnsureTextObject<T>(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        TextAnchor alignment
    )
        where T : Component
    {
        Transform existing = parent.Find(objectName);
        bool createdObject = existing == null;
        GameObject textObject = createdObject ? new GameObject(objectName) : existing.gameObject;
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
        {
            text = textObject.AddComponent<Text>();

            text.font = GetBuiltinUiFont();
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = alignment;
        }

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        if (createdObject)
        {
            rectTransform.anchorMin =
                alignment == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.pivot =
                alignment == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(260f, 42f);
        }

        T component = textObject.GetComponent<T>();
        return component != null ? component : textObject.AddComponent<T>();
    }

    private static PopupController EnsurePopup(Transform parent)
    {
        Transform existingRoot = parent.Find("PopupRoot");
        bool createdRoot = existingRoot == null;
        GameObject popupRoot = createdRoot ? new GameObject("PopupRoot") : existingRoot.gameObject;
        popupRoot.transform.SetParent(parent, false);

        RectTransform rootRect = popupRoot.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = popupRoot.AddComponent<RectTransform>();
        }

        if (createdRoot)
        {
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(160f, 80f);
        }

        Transform existingText = popupRoot.transform.Find("ScorePopupText");
        bool createdTextObject = existingText == null;
        GameObject textObject = createdTextObject
            ? new GameObject("ScorePopupText")
            : existingText.gameObject;
        textObject.transform.SetParent(popupRoot.transform, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 42;
            text.color = new Color(1f, 0.86f, 0.22f);
            text.alignment = TextAlignmentOptions.Center;
            text.text = "+1";
        }

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        if (createdTextObject)
        {
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        PopupController controller = popupRoot.GetComponent<PopupController>();
        return controller != null ? controller : popupRoot.AddComponent<PopupController>();
    }

    private static Font GetBuiltinUiFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void EnsureEventSystem()
    {
        EventSystem existingEventSystem = Object.FindObjectOfType<EventSystem>();
        if (existingEventSystem != null)
        {
            if (existingEventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                existingEventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static StartScreenView EnsureStartScreen(Transform parent)
    {
        Transform existing = parent.Find("StartScreen");
        if (existing != null)
        {
            StartScreenView existingView = existing.GetComponent<StartScreenView>();
            return existingView != null
                ? existingView
                : existing.gameObject.AddComponent<StartScreenView>();
        }

        GameObject screenObject = EnsurePanel(
            parent,
            "StartScreen",
            new Color(0.05f, 0.08f, 0.11f, 0.9f)
        );
        StartScreenView view = screenObject.GetComponent<StartScreenView>();
        return view != null ? view : screenObject.AddComponent<StartScreenView>();
    }

    private static FinishScreenView EnsureFinishScreen(Transform parent)
    {
        FinishScreenView sceneView = Object.FindObjectOfType<FinishScreenView>(true);
        if (sceneView != null)
        {
            return sceneView;
        }

        Transform existing = parent.Find("FinishScreen");
        if (existing != null)
        {
            FinishScreenView existingView = existing.GetComponent<FinishScreenView>();
            return existingView != null
                ? existingView
                : existing.gameObject.AddComponent<FinishScreenView>();
        }

        GameObject screenObject = EnsurePanel(
            parent,
            "FinishScreen",
            new Color(0.08f, 0.05f, 0.08f, 0.92f)
        );
        FinishScreenView view = screenObject.GetComponent<FinishScreenView>();
        return view != null ? view : screenObject.AddComponent<FinishScreenView>();
    }

    private static PauseMenuView EnsurePauseMenu(Transform parent)
    {
        Transform existing = parent.Find("PauseMenu");
        if (existing != null)
        {
            PauseMenuView existingView = existing.GetComponent<PauseMenuView>();
            return existingView != null
                ? existingView
                : existing.gameObject.AddComponent<PauseMenuView>();
        }

        GameObject screenObject = EnsurePanel(
            parent,
            "PauseMenu",
            new Color(0.03f, 0.04f, 0.05f, 0.86f)
        );

        Transform contentRoot = EnsureVerticalContentRoot(
            screenObject.transform,
            "PauseMenuContent",
            new Vector2(520f, 320f)
        );
        EnsureLayoutText(
            contentRoot,
            "PauseMessageText",
            "Paused",
            42,
            new Vector2(480f, 72f),
            TextAnchor.MiddleCenter
        );
        EnsureLayoutButton(
            contentRoot,
            "BackButton",
            "Back",
            new Vector2(280f, 58f)
        );
        EnsureLayoutButton(
            contentRoot,
            "QuitGameButton",
            "Quit Game",
            new Vector2(280f, 58f)
        );

        PauseMenuView view = screenObject.GetComponent<PauseMenuView>();
        return view != null ? view : screenObject.AddComponent<PauseMenuView>();
    }

    private static GameObject EnsurePanel(
        Transform parent,
        string objectName,
        Color backgroundColor
    )
    {
        Transform existing = parent.Find(objectName);
        GameObject panelObject =
            existing != null ? existing.gameObject : new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        Image image = panelObject.GetComponent<Image>();
        if (image == null)
        {
            image = panelObject.AddComponent<Image>();
        }

        image.color = backgroundColor;

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return panelObject;
    }

    private static Text EnsureCenteredText(
        Transform parent,
        string objectName,
        string defaultText,
        Vector2 anchoredPosition,
        int fontSize,
        Vector2 size
    )
    {
        Transform existing = parent.Find(objectName);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
        {
            text = textObject.AddComponent<Text>();
        }

        text.font = GetBuiltinUiFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = defaultText;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        return text;
    }

    private static Button EnsureButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        Transform existing = parent.Find(objectName);
        bool createdObject = existing == null;
        GameObject buttonObject = createdObject ? new GameObject(objectName) : existing.gameObject;
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        if (image == null)
        {
            image = buttonObject.AddComponent<Image>();
        }

        bool hasCustomSprite = image.sprite != null;
        Color normalColor = hasCustomSprite ? Color.white : new Color(0.18f, 0.54f, 0.34f, 0.95f);
        Color highlightedColor = hasCustomSprite
            ? new Color(0.95f, 0.95f, 0.95f, 1f)
            : new Color(0.24f, 0.68f, 0.42f, 0.98f);
        Color pressedColor = hasCustomSprite
            ? new Color(0.82f, 0.82f, 0.82f, 1f)
            : new Color(0.12f, 0.42f, 0.25f, 0.98f);
        image.color = normalColor;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        if (createdObject)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        EnsureButtonLabel(buttonObject.transform, label);
        return button;
    }

    private static void EnsureButtonLabel(Transform buttonTransform, string label)
    {
        Transform existing = buttonTransform.Find("Text");
        GameObject textObject = existing != null ? existing.gameObject : new GameObject("Text");
        textObject.transform.SetParent(buttonTransform, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.text = label;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Transform EnsureVerticalContentRoot(
        Transform parent,
        string objectName,
        Vector2 size
    )
    {
        Transform existing = parent.Find(objectName);
        GameObject rootObject = existing != null ? existing.gameObject : new GameObject(objectName);
        rootObject.transform.SetParent(parent, false);

        RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = rootObject.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = size;

        VerticalLayoutGroup layoutGroup = rootObject.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = rootObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 18f;
        layoutGroup.padding = new RectOffset(24, 24, 24, 24);

        ContentSizeFitter fitter = rootObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = rootObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rootObject.transform;
    }

    private static TMP_Text EnsureLayoutText(
        Transform parent,
        string objectName,
        string defaultText,
        int fontSize,
        Vector2 size,
        TextAnchor alignment
    )
    {
        Transform existing = parent.Find(objectName);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = ToTmpAlignment(alignment);
        text.text = defaultText;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = textObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        return text;
    }

    private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
    {
        return alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center
        };
    }

    private static Button EnsureLayoutButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 size
    )
    {
        Transform existing = parent.Find(objectName);
        GameObject buttonObject =
            existing != null ? existing.gameObject : new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        if (image == null)
        {
            image = buttonObject.AddComponent<Image>();
        }

        image.color = new Color(0.18f, 0.54f, 0.34f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.24f, 0.68f, 0.42f, 0.98f);
        colors.pressedColor = new Color(0.12f, 0.42f, 0.25f, 0.98f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        EnsureButtonLabel(buttonObject.transform, label);
        return button;
    }

    private static void EnsureHistoryTable(Transform parent, Vector2 size)
    {
        Transform existing = parent.Find("HistoryTableRoot");
        GameObject tableObject =
            existing != null ? existing.gameObject : new GameObject("HistoryTableRoot");
        tableObject.transform.SetParent(parent, false);

        RectTransform rectTransform = tableObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = tableObject.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        LayoutElement layoutElement = tableObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = tableObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;

        VerticalLayoutGroup layoutGroup = tableObject.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = tableObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 8f;
        layoutGroup.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = tableObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = tableObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }
}
