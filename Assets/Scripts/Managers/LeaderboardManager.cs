using UnityEngine;
using System;

/// <summary>
/// Data structure for storing personal best scores per difficulty.
/// Each difficulty (Easy, Medium, Hard) has its own set of bests.
/// </summary>
[System.Serializable]
public class DifficultyBestData
{
    public int todayBest;
    public string todayDate;
    public int weekBest;
    public int weekNumber;
    public int weekYear;
    public int allTimeBest;

    public DifficultyBestData()
    {
        todayBest = 0;
        todayDate = "";
        weekBest = 0;
        weekNumber = 0;
        weekYear = 0;
        allTimeBest = 0;
    }
}

/// <summary>
/// Container for all difficulty leaderboards.
/// </summary>
[System.Serializable]
public class AllLeaderboardData
{
    public DifficultyBestData easyBests = new DifficultyBestData();
    public DifficultyBestData mediumBests = new DifficultyBestData();
    public DifficultyBestData hardBests = new DifficultyBestData();
}

/// <summary>
/// Manages personal best scores for each difficulty level.
/// Separate leaderboards for Easy, Medium, and Hard.
/// Tracks: today's best, this week's best, and all-time best.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string SAVE_KEY = "FlappyJumpLeaderboards";

    private AllLeaderboardData allData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
            CheckAndResetPeriods();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Gets the data for a specific difficulty.
    /// </summary>
    DifficultyBestData GetDataForDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            case 0: return allData.easyBests;
            case 2: return allData.hardBests;
            default: return allData.mediumBests;
        }
    }

    /// <summary>
    /// Checks and resets daily/weekly periods for all difficulties.
    /// </summary>
    void CheckAndResetPeriods()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        int currentWeek = GetWeekOfYear(DateTime.Now);
        int currentYear = DateTime.Now.Year;

        CheckAndResetDifficulty(allData.easyBests, today, currentWeek, currentYear);
        CheckAndResetDifficulty(allData.mediumBests, today, currentWeek, currentYear);
        CheckAndResetDifficulty(allData.hardBests, today, currentWeek, currentYear);

        SaveData();
    }

    void CheckAndResetDifficulty(DifficultyBestData data, string today, int currentWeek, int currentYear)
    {
        if (data.todayDate != today)
        {
            data.todayBest = 0;
            data.todayDate = today;
        }

        if (data.weekNumber != currentWeek || data.weekYear != currentYear)
        {
            data.weekBest = 0;
            data.weekNumber = currentWeek;
            data.weekYear = currentYear;
        }
    }

    int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstDay,
            DayOfWeek.Monday);
    }

    /// <summary>
    /// Submits a score for the current difficulty.
    /// </summary>
    public bool SubmitScore(int score)
    {
        int difficulty = 1;
        if (DifficultyManager.Instance != null)
        {
            difficulty = DifficultyManager.Instance.GetDifficulty();
        }

        return SubmitScore(score, difficulty);
    }

    /// <summary>
    /// Submits a score for a specific difficulty.
    /// </summary>
    public bool SubmitScore(int score, int difficulty)
    {
        if (score <= 0) return false;

        DifficultyBestData data = GetDataForDifficulty(difficulty);
        bool beatAnyBest = false;

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        int currentWeek = GetWeekOfYear(DateTime.Now);
        int currentYear = DateTime.Now.Year;

        // Check today's best
        if (data.todayDate != today)
        {
            data.todayBest = 0;
            data.todayDate = today;
        }

        if (score > data.todayBest)
        {
            data.todayBest = score;
            beatAnyBest = true;
        }

        // Check week's best
        if (data.weekNumber != currentWeek || data.weekYear != currentYear)
        {
            data.weekBest = 0;
            data.weekNumber = currentWeek;
            data.weekYear = currentYear;
        }

        if (score > data.weekBest)
        {
            data.weekBest = score;
            beatAnyBest = true;
        }

        // Check all-time best
        if (score > data.allTimeBest)
        {
            data.allTimeBest = score;
            beatAnyBest = true;
        }

        SaveData();
        return beatAnyBest;
    }

    /// <summary>
    /// Gets today's best for current difficulty.
    /// </summary>
    public int GetTodayBest()
    {
        int difficulty = DifficultyManager.Instance?.GetDifficulty() ?? 1;
        return GetTodayBest(difficulty);
    }

    public int GetTodayBest(int difficulty)
    {
        CheckAndResetPeriods();
        return GetDataForDifficulty(difficulty).todayBest;
    }

    /// <summary>
    /// Gets this week's best for current difficulty.
    /// </summary>
    public int GetWeekBest()
    {
        int difficulty = DifficultyManager.Instance?.GetDifficulty() ?? 1;
        return GetWeekBest(difficulty);
    }

    public int GetWeekBest(int difficulty)
    {
        CheckAndResetPeriods();
        return GetDataForDifficulty(difficulty).weekBest;
    }

    /// <summary>
    /// Gets all-time best for current difficulty.
    /// </summary>
    public int GetAllTimeBest()
    {
        int difficulty = DifficultyManager.Instance?.GetDifficulty() ?? 1;
        return GetAllTimeBest(difficulty);
    }

    public int GetAllTimeBest(int difficulty)
    {
        return GetDataForDifficulty(difficulty).allTimeBest;
    }

    private void SaveData()
    {
        string json = JsonUtility.ToJson(allData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            allData = JsonUtility.FromJson<AllLeaderboardData>(json);
        }
        else
        {
            allData = new AllLeaderboardData();
        }
    }

    /// <summary>
    /// Clears all leaderboard data.
    /// </summary>
    public void ClearAllData()
    {
        allData = new AllLeaderboardData();
        SaveData();
    }
}
