using UnityEngine;
using System;

/// <summary>
/// Data structure for storing personal best scores.
/// Tracks today's best, this week's best, and all-time best.
/// </summary>
[System.Serializable]
public class PersonalBestData
{
    // Today's best score
    public int todayBest;
    // Date when today's best was set (to reset daily)
    public string todayDate;

    // This week's best score
    public int weekBest;
    // Week number when week's best was set (to reset weekly)
    public int weekNumber;
    // Year of the week (to handle year transitions)
    public int weekYear;

    // All-time best score
    public int allTimeBest;

    public PersonalBestData()
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
/// Manages personal best scores: today, this week, and all-time.
/// Automatically resets daily and weekly bests when appropriate.
/// Uses PlayerPrefs to persist data between sessions.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    // Singleton instance for global access
    public static LeaderboardManager Instance { get; private set; }

    // Key used to store data in PlayerPrefs
    private const string SAVE_KEY = "FlappyBallPersonalBests";

    // Personal best data
    private PersonalBestData data;

    /// <summary>
    /// Unity Awake - sets up singleton and loads saved data.
    /// </summary>
    void Awake()
    {
        // Singleton pattern
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
    /// Checks if daily or weekly periods have passed and resets scores accordingly.
    /// </summary>
    void CheckAndResetPeriods()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        int currentWeek = GetWeekOfYear(DateTime.Now);
        int currentYear = DateTime.Now.Year;

        // Check if it's a new day
        if (data.todayDate != today)
        {
            Debug.Log($"LeaderboardManager: New day detected. Resetting today's best. (Old: {data.todayDate}, New: {today})");
            data.todayBest = 0;
            data.todayDate = today;
        }

        // Check if it's a new week
        if (data.weekNumber != currentWeek || data.weekYear != currentYear)
        {
            Debug.Log($"LeaderboardManager: New week detected. Resetting week's best.");
            data.weekBest = 0;
            data.weekNumber = currentWeek;
            data.weekYear = currentYear;
        }

        SaveData();
    }

    /// <summary>
    /// Gets the week number for a given date.
    /// </summary>
    int GetWeekOfYear(DateTime date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date,
            System.Globalization.CalendarWeekRule.FirstDay,
            DayOfWeek.Monday);
    }

    /// <summary>
    /// Submits a score and updates personal bests if applicable.
    /// </summary>
    /// <param name="score">The score achieved</param>
    /// <returns>True if any personal best was beaten</returns>
    public bool SubmitScore(int score)
    {
        if (score <= 0) return false;

        bool beatAnyBest = false;

        // Check and update today's best
        if (score > data.todayBest)
        {
            data.todayBest = score;
            data.todayDate = DateTime.Now.ToString("yyyy-MM-dd");
            beatAnyBest = true;
            Debug.Log($"LeaderboardManager: New today's best: {score}");
        }

        // Check and update week's best
        if (score > data.weekBest)
        {
            data.weekBest = score;
            data.weekNumber = GetWeekOfYear(DateTime.Now);
            data.weekYear = DateTime.Now.Year;
            beatAnyBest = true;
            Debug.Log($"LeaderboardManager: New week's best: {score}");
        }

        // Check and update all-time best
        if (score > data.allTimeBest)
        {
            data.allTimeBest = score;
            beatAnyBest = true;
            Debug.Log($"LeaderboardManager: New all-time best: {score}");
        }

        SaveData();
        return beatAnyBest;
    }

    /// <summary>
    /// Gets today's best score.
    /// </summary>
    public int GetTodayBest()
    {
        CheckAndResetPeriods();
        return data.todayBest;
    }

    /// <summary>
    /// Gets this week's best score.
    /// </summary>
    public int GetWeekBest()
    {
        CheckAndResetPeriods();
        return data.weekBest;
    }

    /// <summary>
    /// Gets all-time best score.
    /// </summary>
    public int GetAllTimeBest()
    {
        return data.allTimeBest;
    }

    /// <summary>
    /// Checks if a score beats any personal best.
    /// </summary>
    public bool IsNewPersonalBest(int score)
    {
        return score > data.todayBest || score > data.weekBest || score > data.allTimeBest;
    }

    /// <summary>
    /// Saves data to PlayerPrefs.
    /// </summary>
    private void SaveData()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads data from PlayerPrefs.
    /// </summary>
    private void LoadData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            data = JsonUtility.FromJson<PersonalBestData>(json);
            Debug.Log($"LeaderboardManager: Loaded data - Today: {data.todayBest}, Week: {data.weekBest}, All-Time: {data.allTimeBest}");
        }
        else
        {
            data = new PersonalBestData();
            Debug.Log("LeaderboardManager: No saved data found, starting fresh");
        }
    }

    /// <summary>
    /// Clears all personal best data (for testing/reset).
    /// </summary>
    public void ClearAllData()
    {
        data = new PersonalBestData();
        SaveData();
        Debug.Log("LeaderboardManager: All data cleared");
    }
}
