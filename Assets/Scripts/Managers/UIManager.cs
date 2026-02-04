using UnityEngine;
using TMPro;

/// <summary>
/// Manages all UI elements including score display, start screen, game over screen,
/// and power-up indicators. Subscribes to game events to update UI based on game state.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel shown before game starts with play button")]
    [SerializeField] private GameObject startPanel;
    [Tooltip("Panel shown after game over with final score and restart option")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Score Display")]
    [Tooltip("Text element displaying current score during gameplay")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [Tooltip("Text element showing final score on game over screen")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Power-Up Indicators (Optional)")]
    [Tooltip("GameObject shown when shield is active")]
    [SerializeField] private GameObject shieldIndicator;
    [Tooltip("GameObject shown when slow motion is active")]
    [SerializeField] private GameObject slowMotionIndicator;
    [Tooltip("GameObject shown when score multiplier is active")]
    [SerializeField] private GameObject multiplierIndicator;
    [Tooltip("Text showing current multiplier value")]
    [SerializeField] private TextMeshProUGUI multiplierText;

    [Header("Instructions")]
    [Tooltip("Text showing tap to play instruction")]
    [SerializeField] private TextMeshProUGUI instructionText;

    /// <summary>
    /// Unity Start - subscribes to game events and initializes UI state.
    /// </summary>
    void Start()
    {
        // Subscribe to GameManager events
        GameManager.Instance.OnScoreChanged.AddListener(UpdateScore);
        GameManager.Instance.OnGameStart.AddListener(OnGameStart);
        GameManager.Instance.OnGameOver.AddListener(OnGameOver);

        // Subscribe to PowerUpManager events if available
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnShieldActivated.AddListener(ShowShieldIndicator);
            PowerUpManager.Instance.OnShieldDeactivated.AddListener(HideShieldIndicator);
            PowerUpManager.Instance.OnSlowMotionActivated.AddListener(ShowSlowMotionIndicator);
            PowerUpManager.Instance.OnSlowMotionDeactivated.AddListener(HideSlowMotionIndicator);
            PowerUpManager.Instance.OnMultiplierActivated.AddListener(ShowMultiplierIndicator);
            PowerUpManager.Instance.OnMultiplierDeactivated.AddListener(HideMultiplierIndicator);
        }

        // Initialize UI state
        InitializeUI();
    }

    /// <summary>
    /// Sets initial UI state.
    /// </summary>
    void InitializeUI()
    {
        // Show start screen, hide game over screen
        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Hide power-up indicators
        HideAllPowerUpIndicators();

        // Set initial score display
        if (scoreText != null) scoreText.text = "0";

        // Show instruction text
        if (instructionText != null) instructionText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Updates the score display when score changes.
    /// </summary>
    /// <param name="score">The new score value to display</param>
    void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            // Show multiplier indicator in score if active
            if (GameManager.Instance.ScoreMultiplier > 1f)
            {
                scoreText.text = score.ToString() + " <color=yellow>x" + GameManager.Instance.ScoreMultiplier.ToString("0.#") + "</color>";
            }
            else
            {
                scoreText.text = score.ToString();
            }
        }
    }

    /// <summary>
    /// Called when game starts - hides menu panels and shows game UI.
    /// </summary>
    void OnGameStart()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (instructionText != null) instructionText.gameObject.SetActive(false);

        // Reset power-up indicators
        HideAllPowerUpIndicators();
    }

    /// <summary>
    /// Called when game ends - shows game over panel with final score.
    /// </summary>
    void OnGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // Display final score
        if (finalScoreText != null)
        {
            finalScoreText.text = "Score: " + GameManager.Instance.GetScore().ToString();
        }

        // Hide power-up indicators
        HideAllPowerUpIndicators();
    }

    /// <summary>
    /// Button callback for Play button on start screen.
    /// </summary>
    public void OnPlayButton()
    {
        GameManager.Instance.StartGame();
    }

    /// <summary>
    /// Button callback for Restart button on game over screen.
    /// </summary>
    public void OnRestartButton()
    {
        GameManager.Instance.RestartGame();
    }

    #region Power-Up Indicators

    /// <summary>
    /// Hides all power-up indicator UI elements.
    /// </summary>
    void HideAllPowerUpIndicators()
    {
        if (shieldIndicator != null) shieldIndicator.SetActive(false);
        if (slowMotionIndicator != null) slowMotionIndicator.SetActive(false);
        if (multiplierIndicator != null) multiplierIndicator.SetActive(false);
    }

    /// <summary>
    /// Shows the shield active indicator.
    /// </summary>
    void ShowShieldIndicator()
    {
        if (shieldIndicator != null) shieldIndicator.SetActive(true);
    }

    /// <summary>
    /// Hides the shield active indicator.
    /// </summary>
    void HideShieldIndicator()
    {
        if (shieldIndicator != null) shieldIndicator.SetActive(false);
    }

    /// <summary>
    /// Shows the slow motion active indicator.
    /// </summary>
    void ShowSlowMotionIndicator()
    {
        if (slowMotionIndicator != null) slowMotionIndicator.SetActive(true);
    }

    /// <summary>
    /// Hides the slow motion active indicator.
    /// </summary>
    void HideSlowMotionIndicator()
    {
        if (slowMotionIndicator != null) slowMotionIndicator.SetActive(false);
    }

    /// <summary>
    /// Shows the score multiplier active indicator.
    /// </summary>
    void ShowMultiplierIndicator()
    {
        if (multiplierIndicator != null)
        {
            multiplierIndicator.SetActive(true);
            if (multiplierText != null)
            {
                multiplierText.text = "x" + GameManager.Instance.ScoreMultiplier.ToString("0.#");
            }
        }
    }

    /// <summary>
    /// Hides the score multiplier active indicator.
    /// </summary>
    void HideMultiplierIndicator()
    {
        if (multiplierIndicator != null) multiplierIndicator.SetActive(false);
    }

    #endregion
}
