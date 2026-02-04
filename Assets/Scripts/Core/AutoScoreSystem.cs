using UnityEngine;

/// <summary>
/// Automatically increments the player's score at regular intervals during gameplay.
/// Score is added based on time survived, not obstacles passed.
/// </summary>
public class AutoScoreSystem : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time interval between score increments (in seconds)")]
    [SerializeField] private float scoreInterval = 1f;
    [Tooltip("Points added per interval")]
    [SerializeField] private int pointsPerInterval = 1;

    // Timer tracking
    private float scoreTimer = 0f;
    // Controls if scoring is active
    private bool isScoring = false;

    /// <summary>
    /// Unity Start - subscribes to game events.
    /// </summary>
    void Start()
    {
        // Start scoring when game begins
        GameManager.Instance.OnGameStart.AddListener(OnGameStart);
        // Stop scoring when game ends
        GameManager.Instance.OnGameOver.AddListener(OnGameOver);
    }

    /// <summary>
    /// Unity Update - handles score timing.
    /// </summary>
    void Update()
    {
        if (!isScoring) return;

        // Accumulate time (uses deltaTime so slow motion affects score rate)
        scoreTimer += Time.deltaTime;

        // Add score at each interval
        if (scoreTimer >= scoreInterval)
        {
            GameManager.Instance.AddScore(pointsPerInterval);
            scoreTimer = 0f;
        }
    }

    /// <summary>
    /// Called when game starts - enables auto-scoring.
    /// </summary>
    void OnGameStart()
    {
        isScoring = true;
        scoreTimer = 0f;
    }

    /// <summary>
    /// Called when game ends - disables auto-scoring.
    /// </summary>
    void OnGameOver()
    {
        isScoring = false;
    }

    /// <summary>
    /// Sets the scoring interval dynamically (for difficulty adjustments).
    /// </summary>
    public void SetScoreInterval(float interval)
    {
        scoreInterval = Mathf.Max(0.1f, interval);
    }

    /// <summary>
    /// Sets the points per interval dynamically.
    /// </summary>
    public void SetPointsPerInterval(int points)
    {
        pointsPerInterval = Mathf.Max(1, points);
    }
}
