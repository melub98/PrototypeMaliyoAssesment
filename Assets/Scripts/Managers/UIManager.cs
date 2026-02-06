using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI elements for Flappy Jump including score display,
/// multiplier display, start screen, and game over screen.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI Panels")]
    [Tooltip("Panel shown before game starts")]
    [SerializeField] private GameObject startPanel;

    [Tooltip("Panel shown after game over")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button restartButton;

    [Header("Score Display")]
    [Tooltip("Current score during gameplay")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Final score on game over screen")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Shield Indicator")]
    [Tooltip("Shown when player has a shield")]
    [SerializeField] private GameObject shieldIndicator;

    [Header("Instructions")]
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Achievements")]
    [SerializeField] private Button achievementsButton;
    [SerializeField] private AchievementShowcaseUI achievementShowcase;

    [Header("Fail Indicator")]
    [Tooltip("X icon shown on screen when player dies or misses a hoop")]
    [SerializeField] private GameObject failIcon;

    [Tooltip("How long the X icon stays on screen (seconds)")]
    [SerializeField] private float failIconDuration = 1f;

    #endregion

    private float failIconTimer = 0f;
    private bool gameOverPending = false;

    #region Unity Lifecycle

    void Start()
    {
        SetupButtons();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScore);
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnDeath);
            // Listen to OnShowGameOverUI instead of OnGameOver
            // This delays the UI until ball hits the floor after death
            GameManager.Instance.OnShowGameOverUI.AddListener(OnGameOver);
        }

        InitializeUI();
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OnPlayButton);
        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartButton);
        if (achievementsButton != null) achievementsButton.onClick.RemoveListener(OnAchievementsButton);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(UpdateScore);
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnDeath);
            GameManager.Instance.OnShowGameOverUI.RemoveListener(OnGameOver);
        }
    }

    #endregion

    #region Setup

    void SetupButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButton);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButton);
        }

        if (achievementsButton != null)
        {
            achievementsButton.onClick.RemoveAllListeners();
            achievementsButton.onClick.AddListener(OnAchievementsButton);
        }
    }

    void InitializeUI()
    {
        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (shieldIndicator != null) shieldIndicator.SetActive(false);
        if (failIcon != null) failIcon.SetActive(false);
        if (scoreText != null) scoreText.text = "0";
        if (instructionText != null) instructionText.gameObject.SetActive(true);
    }

    void Update()
    {
        // Auto-hide fail icon after duration
        if (failIconTimer > 0f)
        {
            failIconTimer -= Time.deltaTime;
            if (failIconTimer <= 0f)
            {
                if (failIcon != null) failIcon.SetActive(false);

                // Show game over panel now that X has disappeared
                if (gameOverPending)
                {
                    gameOverPending = false;
                    ShowGameOverPanel();
                }
            }
        }
    }

    #endregion

    #region Score Display

    void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    #endregion

    #region Game State Handlers

    void OnGameStart()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (shieldIndicator != null) shieldIndicator.SetActive(false);
        if (failIcon != null) failIcon.SetActive(false);
        failIconTimer = 0f;
        gameOverPending = false;
    }

    /// <summary>
    /// Called immediately when player dies (miss or boundary hit).
    /// Shows X icon on screen as immediate feedback.
    /// </summary>
    void OnDeath()
    {
        if (failIcon != null)
        {
            failIcon.SetActive(true);
            failIconTimer = failIconDuration;
        }
    }

    void OnGameOver()
    {
        // If the X icon is still showing, wait for it to disappear first
        if (failIconTimer > 0f)
        {
            gameOverPending = true;
            return;
        }

        ShowGameOverPanel();
    }

    void ShowGameOverPanel()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (finalScoreText != null && GameManager.Instance != null)
        {
            finalScoreText.text = "Score: " + GameManager.Instance.GetScore().ToString();
        }
    }

    #endregion

    #region Button Callbacks

    public void OnPlayButton()
    {
        Debug.Log("Play button clicked");
        GameManager.Instance?.StartGame();
    }

    public void OnRestartButton()
    {
        Debug.Log("Restart button clicked");
        GameManager.Instance?.RestartGame();
    }

    public void OnAchievementsButton()
    {
        if (achievementShowcase != null)
            achievementShowcase.ShowPanel();
    }

    #endregion

    #region Shield Indicator

    /// <summary>
    /// Call this to show/hide shield indicator.
    /// </summary>
    public void SetShieldIndicator(bool active)
    {
        if (shieldIndicator != null)
        {
            shieldIndicator.SetActive(active);
        }
    }

    #endregion
}
