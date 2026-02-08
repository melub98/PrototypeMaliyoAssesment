using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

/// <summary>
/// Manages all UI elements for Flappy Jump including score display,
/// multiplier display, start screen, and game over screen.
/// </summary>
public class UIManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CloseWebGLWindow();
#endif

    #region Serialized Fields

    [Header("UI Panels")]
    [Tooltip("Panel shown before game starts")]
    [SerializeField] private GameObject startPanel;

    [Tooltip("Panel shown after game over")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    [Header("Score Display (In-Game)")]
    [Tooltip("Panel that holds score + best score during gameplay")]
    [SerializeField] private GameObject inGameScorePanel;

    [Tooltip("Current score during gameplay")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Label for current score (e.g. 'Score')")]
    [SerializeField] private TextMeshProUGUI scoreLabelText;

    [Tooltip("All-time best score shown during gameplay")]
    [SerializeField] private TextMeshProUGUI inGameBestText;

    [Tooltip("Label for best score (e.g. 'Best')")]
    [SerializeField] private TextMeshProUGUI bestLabelText;

    [Header("Score Display (Game Over)")]
    [Tooltip("Final score on game over screen")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Tooltip("All-time best score on game over screen")]
    [SerializeField] private TextMeshProUGUI allTimeBestText;

    [Header("Shield Indicator")]
    [Tooltip("Shown when player has a shield")]
    [SerializeField] private GameObject shieldIndicator;

    [Header("Instructions")]
    [SerializeField] private TextMeshProUGUI instructionText;

    [Header("Achievements")]
    [SerializeField] private Button achievementsButton;
    [SerializeField] private AchievementShowcaseUI achievementShowcase;

    [Header("Revive")]
    [Tooltip("Panel shown after death offering player a revive")]
    [SerializeField] private GameObject revivePanel;

    [SerializeField] private Button reviveButton;
    [SerializeField] private Button declineReviveButton;

    [Header("Fail Indicator")]
    [Tooltip("X icon shown on screen when player dies or misses a hoop")]
    [SerializeField] private GameObject failIcon;

    [Tooltip("How long the X icon stays on screen (seconds)")]
    [SerializeField] private float failIconDuration = 1f;

    #endregion

    private float failIconTimer = 0f;
    private bool gameOverPending = false;
    private bool revivePending = false;

    #region Unity Lifecycle

    void Start()
    {
        SetupButtons();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScore);
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnDeath);
            GameManager.Instance.OnShowGameOverUI.AddListener(OnGameOver);
            GameManager.Instance.OnRevivePrompt.AddListener(OnRevivePrompt);
            GameManager.Instance.OnRevive.AddListener(OnReviveAccepted);
        }

        InitializeUI();
    }

    void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OnPlayButton);
        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartButton);
        if (achievementsButton != null) achievementsButton.onClick.RemoveListener(OnAchievementsButton);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitButton);
        if (reviveButton != null) reviveButton.onClick.RemoveListener(OnReviveButton);
        if (declineReviveButton != null) declineReviveButton.onClick.RemoveListener(OnDeclineReviveButton);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(UpdateScore);
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnDeath);
            GameManager.Instance.OnShowGameOverUI.RemoveListener(OnGameOver);
            GameManager.Instance.OnRevivePrompt.RemoveListener(OnRevivePrompt);
            GameManager.Instance.OnRevive.RemoveListener(OnReviveAccepted);
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

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButton);
        }

        if (reviveButton != null)
        {
            reviveButton.onClick.RemoveAllListeners();
            reviveButton.onClick.AddListener(OnReviveButton);
        }

        if (declineReviveButton != null)
        {
            declineReviveButton.onClick.RemoveAllListeners();
            declineReviveButton.onClick.AddListener(OnDeclineReviveButton);
        }
    }

    void InitializeUI()
    {
        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (revivePanel != null) revivePanel.SetActive(false);
        if (inGameScorePanel != null) inGameScorePanel.SetActive(false);
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

                // Show revive panel or game over panel now that X has disappeared
                if (revivePending)
                {
                    revivePending = false;
                    ShowRevivePanel();
                }
                else if (gameOverPending)
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

        // Update best score display if player beats it mid-game
        if (inGameBestText != null && LeaderboardManager.Instance != null)
        {
            int allTimeBest = LeaderboardManager.Instance.GetAllTimeBest();
            int displayBest = Mathf.Max(score, allTimeBest);
            inGameBestText.text = displayBest.ToString();
        }
    }

    #endregion

    #region Game State Handlers

    void OnGameStart()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (revivePanel != null) revivePanel.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (shieldIndicator != null) shieldIndicator.SetActive(false);
        if (failIcon != null) failIcon.SetActive(false);
        failIconTimer = 0f;
        gameOverPending = false;
        revivePending = false;

        // Show in-game score panel with best score
        if (inGameScorePanel != null) inGameScorePanel.SetActive(true);
        if (scoreText != null) scoreText.text = "0";

        if (inGameBestText != null && LeaderboardManager.Instance != null)
        {
            int allTimeBest = LeaderboardManager.Instance.GetAllTimeBest();
            inGameBestText.text = allTimeBest.ToString();
        }
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

    /// <summary>
    /// Called when revive prompt should show (after death, before game over).
    /// </summary>
    void OnRevivePrompt()
    {
        // If the X icon is still showing, wait for it to disappear first
        if (failIconTimer > 0f)
        {
            revivePending = true;
            return;
        }

        ShowRevivePanel();
    }

    void ShowRevivePanel()
    {
        if (revivePanel != null) revivePanel.SetActive(true);
    }

    /// <summary>
    /// Called when player accepts revive - hide revive panel, resume gameplay UI.
    /// </summary>
    void OnReviveAccepted()
    {
        if (revivePanel != null) revivePanel.SetActive(false);
        // In-game score panel should already be visible
        if (inGameScorePanel != null) inGameScorePanel.SetActive(true);
    }

    void ShowGameOverPanel()
    {
        // Hide in-game score panel when game over panel shows
        if (inGameScorePanel != null) inGameScorePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        int currentScore = GameManager.Instance != null ? GameManager.Instance.GetScore() : 0;

        if (finalScoreText != null)
        {
            finalScoreText.text = "Score: " + currentScore;
        }

        if (allTimeBestText != null && LeaderboardManager.Instance != null)
        {
            int allTimeBest = LeaderboardManager.Instance.GetAllTimeBest();
            allTimeBestText.text = "Best: " + allTimeBest;
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

    public void OnReviveButton()
    {
        Debug.Log("Revive button clicked");
        GameManager.Instance?.AcceptRevive();
    }

    public void OnDeclineReviveButton()
    {
        Debug.Log("Decline revive button clicked");
        if (revivePanel != null) revivePanel.SetActive(false);
        GameManager.Instance?.DeclineRevive();
    }

    public void OnExitButton()
    {
        Debug.Log("Exit button clicked");

#if UNITY_WEBGL && !UNITY_EDITOR
        CloseWebGLWindow();
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
