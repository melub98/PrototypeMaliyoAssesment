using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays personal best scores on the game over screen.
/// Shows: Today's Best, This Week's Best, All-Time Best.
/// Automatically appears when game ends.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Score Display")]
    [Tooltip("Text showing today's best score")]
    [SerializeField] private TextMeshProUGUI todayBestText;

    [Tooltip("Text showing this week's best score")]
    [SerializeField] private TextMeshProUGUI weekBestText;

    [Tooltip("Text showing all-time best score")]
    [SerializeField] private TextMeshProUGUI allTimeBestText;

    [Tooltip("Text showing current score")]
    [SerializeField] private TextMeshProUGUI currentScoreText;

    [Header("New Record Indicators")]
    [Tooltip("GameObject to show when new today's best is achieved")]
    [SerializeField] private GameObject newTodayBestIndicator;

    [Tooltip("GameObject to show when new week's best is achieved")]
    [SerializeField] private GameObject newWeekBestIndicator;

    [Tooltip("GameObject to show when new all-time best is achieved")]
    [SerializeField] private GameObject newAllTimeBestIndicator;

    [Header("Audio")]
    [Tooltip("Sound played when new record is achieved")]
    [SerializeField] private AudioClip newRecordSound;

    #endregion

    #region Private Fields

    private AudioSource audioSource;
    private int lastScore;
    private bool beatTodayBest;
    private bool beatWeekBest;
    private bool beatAllTimeBest;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Subscribe to game over event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
            Debug.Log("LeaderboardUI: Subscribed to GameManager.OnGameOver");
        }
        else
        {
            Debug.LogError("LeaderboardUI: GameManager.Instance is null!");
        }

        // Check LeaderboardManager
        if (LeaderboardManager.Instance == null)
        {
            Debug.LogError("LeaderboardUI: LeaderboardManager.Instance is null! Make sure LeaderboardManager is in the scene.");
        }

        // Hide new record indicators at start
        HideAllNewRecordIndicators();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }
    }

    #endregion

    #region Game Over Handler

    /// <summary>
    /// Called when game ends - submits score and updates display.
    /// </summary>
    void OnGameOver()
    {
        Debug.Log("LeaderboardUI: OnGameOver called");

        if (GameManager.Instance == null || LeaderboardManager.Instance == null)
        {
            Debug.LogError("LeaderboardUI: Missing GameManager or LeaderboardManager!");
            return;
        }

        // Get current score
        lastScore = GameManager.Instance.GetScore();
        Debug.Log($"LeaderboardUI: Current score = {lastScore}");

        // Store old bests for comparison
        int oldTodayBest = LeaderboardManager.Instance.GetTodayBest();
        int oldWeekBest = LeaderboardManager.Instance.GetWeekBest();
        int oldAllTimeBest = LeaderboardManager.Instance.GetAllTimeBest();

        // Submit score to update records
        bool beatAnyRecord = LeaderboardManager.Instance.SubmitScore(lastScore);

        // Check which records were beaten
        beatTodayBest = lastScore > oldTodayBest && lastScore > 0;
        beatWeekBest = lastScore > oldWeekBest && lastScore > 0;
        beatAllTimeBest = lastScore > oldAllTimeBest && lastScore > 0;

        // Update the display
        UpdateDisplay();

        // Play sound if any record beaten
        if (beatAnyRecord && newRecordSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(newRecordSound);
        }
    }

    #endregion

    #region Display

    /// <summary>
    /// Updates all score displays with current values.
    /// </summary>
    void UpdateDisplay()
    {
        // Update current score
        if (currentScoreText != null)
        {
            currentScoreText.text = lastScore.ToString();
        }

        // Update today's best
        if (todayBestText != null)
        {
            int todayBest = LeaderboardManager.Instance.GetTodayBest();
            todayBestText.text = todayBest.ToString();
        }

        // Update week's best
        if (weekBestText != null)
        {
            int weekBest = LeaderboardManager.Instance.GetWeekBest();
            weekBestText.text = weekBest.ToString();
        }

        // Update all-time best
        if (allTimeBestText != null)
        {
            int allTimeBest = LeaderboardManager.Instance.GetAllTimeBest();
            allTimeBestText.text = allTimeBest.ToString();
        }

        // Show/hide new record indicators
        UpdateNewRecordIndicators();

        Debug.Log($"LeaderboardUI: Display updated - Today: {LeaderboardManager.Instance.GetTodayBest()}, Week: {LeaderboardManager.Instance.GetWeekBest()}, All-Time: {LeaderboardManager.Instance.GetAllTimeBest()}");
    }

    /// <summary>
    /// Shows indicators for any new records achieved.
    /// </summary>
    void UpdateNewRecordIndicators()
    {
        // Hide all first
        HideAllNewRecordIndicators();

        // Show relevant indicators
        if (newTodayBestIndicator != null && beatTodayBest)
        {
            newTodayBestIndicator.SetActive(true);
        }

        if (newWeekBestIndicator != null && beatWeekBest)
        {
            newWeekBestIndicator.SetActive(true);
        }

        if (newAllTimeBestIndicator != null && beatAllTimeBest)
        {
            newAllTimeBestIndicator.SetActive(true);
        }
    }

    /// <summary>
    /// Hides all new record indicators.
    /// </summary>
    void HideAllNewRecordIndicators()
    {
        if (newTodayBestIndicator != null) newTodayBestIndicator.SetActive(false);
        if (newWeekBestIndicator != null) newWeekBestIndicator.SetActive(false);
        if (newAllTimeBestIndicator != null) newAllTimeBestIndicator.SetActive(false);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually refreshes the display with current values.
    /// Call this if you need to update the UI outside of game over.
    /// </summary>
    public void RefreshDisplay()
    {
        if (LeaderboardManager.Instance == null) return;

        if (todayBestText != null)
            todayBestText.text = LeaderboardManager.Instance.GetTodayBest().ToString();

        if (weekBestText != null)
            weekBestText.text = LeaderboardManager.Instance.GetWeekBest().ToString();

        if (allTimeBestText != null)
            allTimeBestText.text = LeaderboardManager.Instance.GetAllTimeBest().ToString();

        HideAllNewRecordIndicators();
    }

    #endregion
}
