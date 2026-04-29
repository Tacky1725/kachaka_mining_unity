using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FinishScreenView : MonoBehaviour
{
    [SerializeField] private Text currentScoreText;
    [SerializeField] private Transform historyTableRoot;
    [SerializeField] private Button backButton;

    private readonly List<GameObject> historyRows = new List<GameObject>();

    private Action backRequested;

    private void Awake()
    {
        if (currentScoreText == null)
        {
            Transform scoreTransform = FindDescendant("CurrentScoreText");
            if (scoreTransform != null)
            {
                currentScoreText = scoreTransform.GetComponent<Text>();
            }
        }

        if (historyTableRoot == null)
        {
            historyTableRoot = FindDescendant("HistoryTableRoot");
        }

        if (backButton == null)
        {
            Transform buttonTransform = FindDescendant("BackButton");
            if (buttonTransform != null)
            {
                backButton = buttonTransform.GetComponent<Button>();
            }
        }
    }

    public void Bind(Action onBackRequested)
    {
        backRequested = onBackRequested;

        if (backButton == null)
        {
            return;
        }

        backButton.onClick.RemoveListener(HandleBackButtonClicked);
        backButton.onClick.AddListener(HandleBackButtonClicked);
    }

    public void ShowResults(int currentScore, IReadOnlyList<ScoreHistoryEntry> scoreHistory)
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = $"Score: {currentScore}";
        }

        RebuildHistoryTable(scoreHistory);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void RebuildHistoryTable(IReadOnlyList<ScoreHistoryEntry> scoreHistory)
    {
        if (historyTableRoot == null)
        {
            return;
        }

        ClearHistoryRows();
        CreateHeaderRow();

        if (scoreHistory == null || scoreHistory.Count == 0)
        {
            CreateDataRow("-", "No scores yet", "-");
            return;
        }

        int count = Mathf.Min(5, scoreHistory.Count);
        for (int index = 0; index < count; index++)
        {
            ScoreHistoryEntry entry = scoreHistory[index];
            if (entry == null)
            {
                continue;
            }

            string rankLabel = GetRankLabel(index + 1);
            string pointsLabel = entry.points == 1 ? "1 point" : $"{entry.points} points";
            CreateDataRow(rankLabel, pointsLabel, entry.playedAtJst);
        }
    }

    private string GetRankLabel(int rank)
    {
        if (rank % 100 is >= 11 and <= 13)
        {
            return $"{rank}th";
        }

        return (rank % 10) switch
        {
            1 => $"{rank}st",
            2 => $"{rank}nd",
            3 => $"{rank}rd",
            _ => $"{rank}th"
        };
    }

    private void HandleBackButtonClicked()
    {
        backRequested?.Invoke();
    }

    private void ClearHistoryRows()
    {
        for (int index = historyRows.Count - 1; index >= 0; index--)
        {
            GameObject rowObject = historyRows[index];
            if (rowObject != null)
            {
                Destroy(rowObject);
            }
        }

        historyRows.Clear();
    }

    private void CreateHeaderRow()
    {
        GameObject headerRow = CreateRowObject("HistoryHeaderRow");
        CreateCell(headerRow.transform, "RankHeader", "Rank", 120f, FontStyle.Bold);
        CreateCell(headerRow.transform, "ScoreHeader", "Score", 200f, FontStyle.Bold);
        CreateCell(headerRow.transform, "PlayedAtHeader", "Played at", 320f, FontStyle.Bold);
    }

    private void CreateDataRow(string rank, string score, string playedAt)
    {
        GameObject rowObject = CreateRowObject("HistoryDataRow");
        CreateCell(rowObject.transform, "RankCell", rank, 120f, FontStyle.Normal);
        CreateCell(rowObject.transform, "ScoreCell", score, 200f, FontStyle.Normal);
        CreateCell(rowObject.transform, "PlayedAtCell", playedAt, 320f, FontStyle.Normal);
    }

    private GameObject CreateRowObject(string objectName)
    {
        GameObject rowObject = new GameObject(objectName);
        rowObject.transform.SetParent(historyTableRoot, false);

        RectTransform rectTransform = rowObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(720f, 32f);

        HorizontalLayoutGroup layoutGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 12f;

        LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 720f;
        layoutElement.preferredHeight = 32f;

        historyRows.Add(rowObject);
        return rowObject;
    }

    private void CreateCell(Transform parent, string objectName, string value, float width, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = value;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(width, 32f);

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = 32f;
    }

    private Transform FindDescendant(string objectName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text != null && text.gameObject.name == objectName)
            {
                return text.transform;
            }
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == objectName)
            {
                return button.transform;
            }
        }

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in transforms)
        {
            if (child != null && child.gameObject.name == objectName)
            {
                return child;
            }
        }

        return null;
    }
}
