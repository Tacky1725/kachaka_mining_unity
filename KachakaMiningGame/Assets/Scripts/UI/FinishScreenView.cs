using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinishScreenView : MonoBehaviour
{
    private const int HistoryColumnCount = 3;
    private const float DefaultTableWidth = 720f;
    private const float RowHeight = 32f;

    [SerializeField] private Text currentScoreText;
    [SerializeField] private Transform historyTableRoot;
    [SerializeField] private TMP_FontAsset historyTableFont;
    [SerializeField, Min(1f)] private float historyTableFontSize = 24f;
    [SerializeField, Min(0f)] private float historyRankColumnWidth = 160f;
    [SerializeField, Min(0f)] private float historyScoreColumnWidth = 200f;
    [SerializeField, Min(0f)] private float historyPlayedAtColumnWidth = 416f;
    [SerializeField, Min(0f)] private float historyColumnGap = 12f;
    [SerializeField] private Button backButton;

    private readonly List<GameObject> historyRows = new List<GameObject>();
    private readonly List<TMP_Text[]> historyRowCells = new List<TMP_Text[]>();

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

        AlignHistoryTableRootToCenter();
        DisableHistoryTableRootText();

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
        AlignHistoryTableRootToCenter();
        DisableHistoryTableRootText();
        CreateHeaderRow();

        if (scoreHistory == null || scoreHistory.Count == 0)
        {
            CreateDataRow("-", "No scores yet", "-");
            ApplyAutoColumnWidths();
            return;
        }

        int count = Mathf.Min(5, scoreHistory.Count);
        bool hasVisibleEntry = false;
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
            hasVisibleEntry = true;
        }

        if (!hasVisibleEntry)
        {
            CreateDataRow("-", "No scores yet", "-");
        }

        ApplyAutoColumnWidths();
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
        historyRowCells.Clear();
    }

    private void CreateHeaderRow()
    {
        GameObject headerRow = CreateRowObject("HistoryHeaderRow");
        TMP_Text[] cells = new TMP_Text[HistoryColumnCount];
        cells[0] = CreateCell(headerRow.transform, "RankHeader", "Rank", FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        cells[1] = CreateCell(headerRow.transform, "ScoreHeader", "Score", FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        cells[2] = CreateCell(headerRow.transform, "PlayedAtHeader", "Played at", FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        historyRowCells.Add(cells);
    }

    private void CreateDataRow(string rank, string score, string playedAt)
    {
        GameObject rowObject = CreateRowObject("HistoryDataRow");
        TMP_Text[] cells = new TMP_Text[HistoryColumnCount];
        cells[0] = CreateCell(rowObject.transform, "RankCell", rank, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        cells[1] = CreateCell(rowObject.transform, "ScoreCell", score, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        cells[2] = CreateCell(rowObject.transform, "PlayedAtCell", playedAt, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        historyRowCells.Add(cells);
    }

    private GameObject CreateRowObject(string objectName)
    {
        GameObject rowObject = new GameObject(objectName);
        rowObject.layer = historyTableRoot.gameObject.layer;
        rowObject.transform.SetParent(historyTableRoot, false);

        RectTransform rectTransform = rowObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(GetTableWidth(), RowHeight);

        LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = GetTableWidth();
        layoutElement.preferredHeight = RowHeight;

        historyRows.Add(rowObject);
        return rowObject;
    }

    private TMP_Text CreateCell(Transform parent, string objectName, string value, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.layer = parent.gameObject.layer;
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = historyTableFont != null ? historyTableFont : TMP_Settings.defaultFontAsset;
        text.fontSize = historyTableFontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = value;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(0f, 0f);

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 0f;
        layoutElement.preferredHeight = RowHeight;

        return text;
    }

    private void ApplyAutoColumnWidths()
    {
        if (historyRowCells.Count == 0)
        {
            return;
        }

        float[] columnWidths = GetConfiguredColumnWidths();

        foreach (TMP_Text[] rowCells in historyRowCells)
        {
            float columnX = 0f;
            for (int columnIndex = 0; columnIndex < rowCells.Length; columnIndex++)
            {
                TMP_Text text = rowCells[columnIndex];
                LayoutElement layoutElement = text.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = columnWidths[columnIndex];

                RectTransform rectTransform = text.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(columnX, 0f);
                rectTransform.sizeDelta = new Vector2(columnWidths[columnIndex], 0f);

                text.ForceMeshUpdate();
                columnX += columnWidths[columnIndex] + historyColumnGap;
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private float[] GetConfiguredColumnWidths()
    {
        return new[]
        {
            historyRankColumnWidth,
            historyScoreColumnWidth,
            historyPlayedAtColumnWidth
        };
    }

    private float GetTableWidth()
    {
        if (historyTableRoot == null)
        {
            return DefaultTableWidth;
        }

        RectTransform tableRect = historyTableRoot as RectTransform;
        if (tableRect != null && tableRect.rect.width > 0f)
        {
            return tableRect.rect.width;
        }

        LayoutElement layoutElement = historyTableRoot.GetComponent<LayoutElement>();
        if (layoutElement != null && layoutElement.preferredWidth > 0f)
        {
            return layoutElement.preferredWidth;
        }

        return tableRect != null && tableRect.sizeDelta.x > 0f
            ? tableRect.sizeDelta.x
            : DefaultTableWidth;
    }

    private void DisableHistoryTableRootText()
    {
        if (historyTableRoot == null)
        {
            return;
        }

        TMP_Text rootText = historyTableRoot.GetComponent<TMP_Text>();
        if (rootText != null)
        {
            rootText.enabled = false;
        }
    }

    private void AlignHistoryTableRootToCenter()
    {
        if (historyTableRoot == null)
        {
            return;
        }

        RectTransform tableRect = historyTableRoot as RectTransform;
        if (tableRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = tableRect.anchoredPosition;
        tableRect.anchorMin = new Vector2(0.5f, tableRect.anchorMin.y);
        tableRect.anchorMax = new Vector2(0.5f, tableRect.anchorMax.y);
        tableRect.pivot = new Vector2(0.5f, tableRect.pivot.y);
        tableRect.anchoredPosition = new Vector2(0f, anchoredPosition.y);
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
