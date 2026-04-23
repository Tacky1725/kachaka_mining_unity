using UnityEngine;
using UnityEngine.UI;

public static class UiBootstrapper
{
    public static void EnsureCanvas(
        out ScoreView scoreView,
        out TimerView timerView,
        out GameStateView stateView,
        out PopupController popupController)
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

        Transform canvasTransform = canvas.transform;
        scoreView = EnsureTextObject<ScoreView>(canvasTransform, "ScoreText", new Vector2(24f, -24f), TextAnchor.UpperLeft);
        timerView = EnsureTextObject<TimerView>(canvasTransform, "TimerText", new Vector2(-24f, -24f), TextAnchor.UpperRight);
        stateView = EnsureTextObject<GameStateView>(canvasTransform, "StateText", new Vector2(24f, -64f), TextAnchor.UpperLeft);
        popupController = EnsurePopup(canvasTransform);
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
}
