using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central game manager that controls the game state and broadcasts events.
/// Uses the Singleton pattern to provide global access throughout the game.
/// Supports score multipliers for power-up integration.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton instance - allows other scripts to access GameManager via GameManager.Instance
    public static GameManager Instance { get; private set; }

    [Header("Events")]
    // Event fired when the game starts - other scripts can subscribe to react to game start
    public UnityEvent OnGameStart = new UnityEvent();
    // Event fired when the player dies - triggers game over UI, stops spawning, etc.
    public UnityEvent OnGameOver = new UnityEvent();
    // Event fired when score changes - UI subscribes to update score display
    public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();

    [Header("Settings")]
    // Base speed for platform movement - can be adjusted in Inspector
    public float gameSpeed = 3f;

    // Current player score - private to prevent external modification
    private int score = 0;
    // Current score multiplier (modified by power-ups)
    private float scoreMultiplier = 1f;
    // Tracks whether the game is currently active
    private bool isPlaying = false;
    // Tracks if game has been started at least once (for first tap detection)
    private bool hasStarted = false;

    // Public read-only property to check if game is in progress
    public bool IsPlaying => isPlaying;
    // Public read-only property to check if game has ever started
    public bool HasStarted => hasStarted;
    // Public read-only property to get current score
    public int Score => score;
    // Public read-only property to get current multiplier
    public float ScoreMultiplier => scoreMultiplier;

    /// <summary>
    /// Returns the current score. Alternative to Score property for method-style access.
    /// </summary>
    public int GetScore() => score;

    /// <summary>
    /// Unity Awake - called when script instance is loaded.
    /// Sets up the singleton instance, destroying duplicates if they exist.
    /// </summary>
    void Awake()
    {
        // Singleton pattern: ensure only one GameManager exists
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Starts a new game session. Resets score, enables gameplay, and notifies listeners.
    /// Called when player makes first tap or presses the Play button.
    /// </summary>
    public void StartGame()
    {
        if (isPlaying) return; // Prevent multiple starts

        isPlaying = true;
        hasStarted = true;
        score = 0;
        scoreMultiplier = 1f;
        Time.timeScale = 1f; // Ensure game runs at normal speed
        OnGameStart.Invoke(); // Notify all listeners (ball, spawner, UI, etc.)
        OnScoreChanged.Invoke(score); // Update UI with initial score of 0
    }

    /// <summary>
    /// Ends the current game session. Called when player hits a platform or boundary.
    /// </summary>
    public void GameOver()
    {
        // Prevent multiple game over calls
        if (!isPlaying) return;
        isPlaying = false;
        Time.timeScale = 1f; // Reset time scale in case slow motion was active
        OnGameOver.Invoke(); // Notify all listeners to stop/show game over state
    }

    /// <summary>
    /// Adds points to the player's score with multiplier applied.
    /// </summary>
    /// <param name="points">Base number of points to add (default is 1)</param>
    public void AddScore(int points = 1)
    {
        if (!isPlaying) return;

        // Apply multiplier and round to nearest integer
        int actualPoints = Mathf.RoundToInt(points * scoreMultiplier);
        score += actualPoints;
        OnScoreChanged.Invoke(score); // Update UI with new score
    }

    /// <summary>
    /// Sets the score multiplier. Used by power-ups.
    /// </summary>
    /// <param name="multiplier">New multiplier value (1 = normal)</param>
    public void SetScoreMultiplier(float multiplier)
    {
        scoreMultiplier = Mathf.Max(1f, multiplier); // Minimum multiplier is 1
    }

    /// <summary>
    /// Resets the score multiplier to default.
    /// </summary>
    public void ResetScoreMultiplier()
    {
        scoreMultiplier = 1f;
    }

    /// <summary>
    /// Restarts the game by reloading the current scene.
    /// This resets all game objects to their initial state.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f; // Ensure time is normal before reload
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
