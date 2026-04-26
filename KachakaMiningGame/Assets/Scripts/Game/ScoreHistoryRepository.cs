using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class ScoreHistoryRepository
{
    private readonly string filePath;

    public ScoreHistoryRepository()
    {
        filePath = Path.Combine(Application.persistentDataPath, "score_history.json");
    }

    public IReadOnlyList<ScoreHistoryEntry> LoadScoresDescending()
    {
        ScoreHistoryData data = LoadData();
        return OrderEntries(data.entries);
    }

    public IReadOnlyList<ScoreHistoryEntry> AppendScoreAndLoadDescending(int score)
    {
        ScoreHistoryData data = LoadData();
        data.entries.Add(new ScoreHistoryEntry
        {
            playedAtJst = GetCurrentJapanTimeString(),
            points = score
        });
        SaveData(data);
        return OrderEntries(data.entries);
    }

    private ScoreHistoryData LoadData()
    {
        if (!File.Exists(filePath))
        {
            return new ScoreHistoryData();
        }

        string json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ScoreHistoryData();
        }

        ScoreHistoryData data = JsonUtility.FromJson<ScoreHistoryData>(json);
        if (data == null || data.entries == null)
        {
            return new ScoreHistoryData();
        }

        return data;
    }

    private void SaveData(ScoreHistoryData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
    }

    private IReadOnlyList<ScoreHistoryEntry> OrderEntries(IEnumerable<ScoreHistoryEntry> entries)
    {
        return entries
            .Where(entry => entry != null)
            .OrderByDescending(entry => entry.points)
            .ThenByDescending(entry => entry.playedAtJst)
            .ToArray();
    }

    private string GetCurrentJapanTimeString()
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        TimeZoneInfo japanTimeZone = FindJapanTimeZone();
        DateTimeOffset japanTime = TimeZoneInfo.ConvertTime(utcNow, japanTimeZone);
        return $"{japanTime:yyyy/MM/dd HH:mm:ss}";
    }

    private TimeZoneInfo FindJapanTimeZone()
    {
        string[] candidateIds =
        {
            "Tokyo Standard Time",
            "Asia/Tokyo"
        };

        foreach (string timeZoneId in candidateIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Japan Standard Time Fallback",
            TimeSpan.FromHours(9),
            "Japan Standard Time",
            "Japan Standard Time");
    }

    [System.Serializable]
    private class ScoreHistoryData
    {
        public List<ScoreHistoryEntry> entries = new List<ScoreHistoryEntry>();
    }
}
