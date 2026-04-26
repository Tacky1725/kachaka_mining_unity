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
        out FinishScreenView finishScreenView)
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
        scoreView = EnsureTextObject<ScoreView>(canvasTransform, "ScoreText", new Vector2(24f, -24f), TextAnchor.UpperLeft);
        timerView = EnsureTextObject<TimerView>(canvasTransform, "TimerText", new Vector2(-24f, -24f), TextAnchor.UpperRight);
        stateView = EnsureTextObject<GameStateView>(canvasTransform, "StateText", new Vector2(24f, -64f), TextAnchor.UpperLeft);
        popupController = EnsurePopup(canvasTransform);
        startScreenView = EnsureStartScreen(canvasTransform);
        finishScreenView = EnsureFinishScreen(canvasTransform);
    }

    private static T EnsureTextObject<T>(Transform parent, string objectName, Vector2 anchoredPosition, TextAnchor alignment) where T : Component
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
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = alignment;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = alignment == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.pivot = alignment == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(260f, 42f);

        T component = textObject.GetComponent<T>();
        return component != null ? component : textObject.AddComponent<T>();
    }

    private static PopupController EnsurePopup(Transform parent)
    {
        Transform existingRoot = parent.Find("PopupRoot");
        GameObject popupRoot = existingRoot != null ? existingRoot.gameObject : new GameObject("PopupRoot");
        popupRoot.transform.SetParent(parent, false);

        RectTransform rootRect = popupRoot.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = popupRoot.AddComponent<RectTransform>();
        }

        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(160f, 80f);

        Transform existingText = popupRoot.transform.Find("ScorePopupText");
        GameObject textObject = existingText != null ? existingText.gameObject : new GameObject("ScorePopupText");
        textObject.transform.SetParent(popupRoot.transform, false);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
        {
            text = textObject.AddComponent<Text>();
        }

        text.font = GetBuiltinUiFont();
        text.fontSize = 42;
        text.color = new Color(1f, 0.86f, 0.22f);
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

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
        GameObject screenObject = EnsurePanel(parent, "StartScreen", new Color(0.05f, 0.08f, 0.11f, 0.9f));
        EnsureCenteredText(screenObject.transform, "TitleText", "Kachaka Mining Game", new Vector2(0f, 80f), 42, new Vector2(520f, 60f));
        EnsureButton(screenObject.transform, "StartButton", "START", new Vector2(0f, -10f), new Vector2(220f, 64f));

        StartScreenView view = screenObject.GetComponent<StartScreenView>();
        return view != null ? view : screenObject.AddComponent<StartScreenView>();
    }

    private static FinishScreenView EnsureFinishScreen(Transform parent)
    {
        GameObject screenObject = EnsurePanel(parent, "FinishScreen", new Color(0.08f, 0.05f, 0.08f, 0.92f));
        Transform contentRoot = EnsureVerticalContentRoot(screenObject.transform, "ContentRoot", new Vector2(860f, 420f));
        EnsureLayoutText(contentRoot, "TitleText", "Game Finished", 40, new Vector2(700f, 60f), TextAnchor.MiddleCenter);
        EnsureLayoutText(contentRoot, "CurrentScoreText", "Score: 0", 34, new Vector2(420f, 50f), TextAnchor.MiddleCenter);
        EnsureHistoryTable(contentRoot, new Vector2(800f, 220f));
        EnsureLayoutButton(contentRoot, "BackButton", "BACK TO START", new Vector2(300f, 58f));

        FinishScreenView view = screenObject.GetComponent<FinishScreenView>();
        return view != null ? view : screenObject.AddComponent<FinishScreenView>();
    }

    private static GameObject EnsurePanel(Transform parent, string objectName, Color backgroundColor)
    {
        Transform existing = parent.Find(objectName);
        GameObject panelObject = existing != null ? existing.gameObject : new GameObject(objectName);
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

    private static Text EnsureCenteredText(Transform parent, string objectName, string defaultText, Vector2 anchoredPosition, int fontSize, Vector2 size)
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

    private static Button EnsureButton(Transform parent, string objectName, string label, Vector2 anchoredPosition, Vector2 size)
    {
        Transform existing = parent.Find(objectName);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(objectName);
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
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        EnsureButtonLabel(buttonObject.transform, label);
        return button;
    }

    private static void EnsureButtonLabel(Transform buttonTransform, string label)
    {
        Transform existing = buttonTransform.Find("Text");
        GameObject textObject = existing != null ? existing.gameObject : new GameObject("Text");
        textObject.transform.SetParent(buttonTransform, false);

        Text text = textObject.GetComponent<Text>();
        if (text == null)
        {
            text = textObject.AddComponent<Text>();
        }

        text.font = GetBuiltinUiFont();
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Transform EnsureVerticalContentRoot(Transform parent, string objectName, Vector2 size)
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

    private static Text EnsureLayoutText(Transform parent, string objectName, string defaultText, int fontSize, Vector2 size, TextAnchor alignment)
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
        text.alignment = alignment;
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

    private static Button EnsureLayoutButton(Transform parent, string objectName, string label, Vector2 size)
    {
        Transform existing = parent.Find(objectName);
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(objectName);
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
        GameObject tableObject = existing != null ? existing.gameObject : new GameObject("HistoryTableRoot");
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
