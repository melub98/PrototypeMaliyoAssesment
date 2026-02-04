using UnityEngine;
using TMPro;

/// <summary>
/// Displays personal best scores on the game over screen.
/// Shows scores for the current difficulty level.
/// Displays: Today's Best, This Week's Best, All-Time Best.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private TextMeshProUGUI todayBestText;
    [SerializeField] private TextMeshProUGUI weekBestText;
    [SerializeField] private TextMeshProUGUI allTimeBestText;

    [Header("Difficulty Display")]
    [SerializeField] private TextMeshProUGUI difficultyLabelText;

    [Header("New Record Indicators")]
    [SerializeField] private GameObject newTodayBestIndicator;
    [SerializeField] private GameObject newWeekBestIndicator;
    [SerializeField] private GameObject newAllTimeBestIndicator;

    [Header("Audio")]
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }

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

    void OnGameOver()
    {
        if (GameManager.Instance == null || LeaderboardManager.Instance == null)
        {
            return;
        }

        lastScore = GameManager.Instance.GetScore();

        // Get old bests for comparison
        int oldTodayBest = LeaderboardManager.Instance.GetTodayBest();
        int oldWeekBest = LeaderboardManager.Instance.GetWeekBest();
        int oldAllTimeBest = LeaderboardManager.Instance.GetAllTimeBest();

        // Submit score
        bool beatAnyRecord = LeaderboardManager.Instance.SubmitScore(lastScore);

        // Check which records were beaten
        beatTodayBest = lastScore > oldTodayBest && lastScore > 0;
        beatWeekBest = lastScore > oldWeekBest && lastScore > 0;
        beatAllTimeBest = lastScore > oldAllTimeBest && lastScore > 0;

        UpdateDisplay();

        if (beatAnyRecord && newRecordSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(newRecordSound);
        }
    }

    #endregion

    #region Display

    void UpdateDisplay()
    {
        // Show current difficulty
        if (difficultyLabelText != null && DifficultyManager.Instance != null)
        {
            string diffName = DifficultyManager.Instance.GetDifficultyName();
            difficultyLabelText.text = $"{diffName} Mode";

            // Color code
            switch (DifficultyManager.Instance.GetDifficulty())
            {
                case 0:
                    difficultyLabelText.color = Color.green;
                    break;
                case 2:
                    difficultyLabelText.color = Color.red;
                    break;
                default:
                    difficultyLabelText.color = Color.yellow;
                    break;
            }
        }

        // Update current score
        if (currentScoreText != null)
        {
            currentScoreText.text = lastScore.ToString();
        }

        // Update today's best
        if (todayBestText != null)
        {
            todayBestText.text = LeaderboardManager.Instance.GetTodayBest().ToString();
        }

        // Update week's best
        if (weekBestText != null)
        {
            weekBestText.text = LeaderboardManager.Instance.GetWeekBest().ToString();
        }

        // Update all-time best
        if (allTimeBestText != null)
        {
            allTimeBestText.text = LeaderboardManager.Instance.GetAllTimeBest().ToString();
        }

        UpdateNewRecordIndicators();
    }

    void UpdateNewRecordIndicators()
    {
        HideAllNewRecordIndicators();

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

    void HideAllNewRecordIndicators()
    {
        if (newTodayBestIndicator != null) newTodayBestIndicator.SetActive(false);
        if (newWeekBestIndicator != null) newWeekBestIndicator.SetActive(false);
        if (newAllTimeBestIndicator != null) newAllTimeBestIndicator.SetActive(false);
    }

    #endregion

    #region Public Methods

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
