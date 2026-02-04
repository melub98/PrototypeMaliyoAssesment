using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a single entry in the leaderboard with player name, score, and date.
/// Serializable so it can be saved/loaded via JSON.
/// </summary>
[System.Serializable]
public class LeaderboardEntry
{
    // Name entered by the player
    public string playerName;
    // Score achieved by the player
    public int score;
    // Date when the score was achieved (formatted as MM/dd/yyyy)
    public string date;

    /// <summary>
    /// Creates a new leaderboard entry with the current date.
    /// </summary>
    /// <param name="name">Player's name</param>
    /// <param name="score">Score achieved</param>
    public LeaderboardEntry(string name, int score)
    {
        this.playerName = name;
        this.score = score;
        this.date = System.DateTime.Now.ToString("MM/dd/yyyy");
    }
}

/// <summary>
/// Container class for serializing the list of leaderboard entries to JSON.
/// </summary>
[System.Serializable]
public class LeaderboardData
{
    // List of all leaderboard entries
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

/// <summary>
/// Manages the leaderboard data including saving, loading, and querying scores.
/// Uses PlayerPrefs to persist data between sessions.
/// Singleton pattern for global access.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    // Singleton instance for global access
    public static LeaderboardManager Instance { get; private set; }

    // Key used to store leaderboard data in PlayerPrefs
    private const string LEADERBOARD_KEY = "FlappyBallLeaderboard";
    // Maximum number of entries to keep in the leaderboard
    private const int MAX_ENTRIES = 10;

    // Container holding all leaderboard entries
    private LeaderboardData leaderboardData;

    /// <summary>
    /// Unity Awake - sets up singleton and loads saved leaderboard data.
    /// </summary>
    void Awake()
    {
        // Singleton pattern: ensure only one LeaderboardManager exists
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Persist across scene loads so leaderboard data is maintained
        DontDestroyOnLoad(gameObject);
        // Load any previously saved leaderboard data
        LoadLeaderboard();
    }

    /// <summary>
    /// Adds a new score to the leaderboard and saves the updated data.
    /// Automatically sorts by score and keeps only top entries.
    /// </summary>
    /// <param name="playerName">Name of the player</param>
    /// <param name="score">Score achieved</param>
    public void AddScore(string playerName, int score)
    {
        // Create new entry with current date
        LeaderboardEntry newEntry = new LeaderboardEntry(playerName, score);
        leaderboardData.entries.Add(newEntry);

        // Sort entries by score (highest first) and keep only top MAX_ENTRIES
        leaderboardData.entries = leaderboardData.entries
            .OrderByDescending(e => e.score)
            .Take(MAX_ENTRIES)
            .ToList();

        // Persist changes to PlayerPrefs
        SaveLeaderboard();
    }

    /// <summary>
    /// Returns the top scores from the leaderboard.
    /// </summary>
    /// <param name="count">Number of entries to return (default 10)</param>
    /// <returns>List of top leaderboard entries</returns>
    public List<LeaderboardEntry> GetTopScores(int count = 10)
    {
        return leaderboardData.entries.Take(count).ToList();
    }

    /// <summary>
    /// Checks if a score qualifies for the leaderboard.
    /// </summary>
    /// <param name="score">Score to check</param>
    /// <returns>True if score would make it onto the leaderboard</returns>
    public bool IsHighScore(int score)
    {
        // If leaderboard isn't full, any score qualifies
        if (leaderboardData.entries.Count < MAX_ENTRIES)
            return true;

        // Otherwise, score must beat the lowest entry
        return score > leaderboardData.entries.Last().score;
    }

    /// <summary>
    /// Determines what rank a score would achieve on the leaderboard.
    /// </summary>
    /// <param name="score">Score to check</param>
    /// <returns>Rank position (1 = first place)</returns>
    public int GetRank(int score)
    {
        int rank = 1;
        // Count how many existing scores are higher
        foreach (var entry in leaderboardData.entries)
        {
            if (score > entry.score)
                return rank;
            rank++;
        }
        return rank;
    }

    /// <summary>
    /// Saves the leaderboard data to PlayerPrefs as JSON.
    /// </summary>
    private void SaveLeaderboard()
    {
        // Convert to JSON and store in PlayerPrefs
        string json = JsonUtility.ToJson(leaderboardData);
        PlayerPrefs.SetString(LEADERBOARD_KEY, json);
        PlayerPrefs.Save(); // Force immediate write to disk
    }

    /// <summary>
    /// Loads leaderboard data from PlayerPrefs.
    /// Creates empty leaderboard if no saved data exists.
    /// </summary>
    private void LoadLeaderboard()
    {
        if (PlayerPrefs.HasKey(LEADERBOARD_KEY))
        {
            // Load and deserialize existing data
            string json = PlayerPrefs.GetString(LEADERBOARD_KEY);
            leaderboardData = JsonUtility.FromJson<LeaderboardData>(json);
        }
        else
        {
            // Create new empty leaderboard
            leaderboardData = new LeaderboardData();
        }
    }

    /// <summary>
    /// Clears all leaderboard entries and saves the empty state.
    /// Useful for testing or reset functionality.
    /// </summary>
    public void ClearLeaderboard()
    {
        leaderboardData.entries.Clear();
        SaveLeaderboard();
    }
}
