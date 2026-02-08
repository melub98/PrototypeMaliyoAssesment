using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central game manager for Flappy Jump.
/// Handles game state, score tracking, and dynamic multiplier system.
/// Multiplier doubles on clean hoop passes, resets on edge touches.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Events")]
    public UnityEvent OnGameStart = new UnityEvent();
    public UnityEvent OnGameOver = new UnityEvent();
    /// <summary>
    /// Fired when game over UI should be shown (after ball hits floor).
    /// UIManager listens to this, not OnGameOver, for delayed UI display.
    /// </summary>
    public UnityEvent OnShowGameOverUI = new UnityEvent();
    public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();
    public UnityEvent<int> OnMultiplierChanged = new UnityEvent<int>();
    public UnityEvent OnCleanPass = new UnityEvent();
    public UnityEvent OnEdgeTouch = new UnityEvent();
    /// <summary>
    /// Fired on every hoop pass. Bool parameter = wasCleanPass.
    /// </summary>
    public UnityEvent<bool> OnHoopPassed = new UnityEvent<bool>();
    /// <summary>
    /// Fired when revive prompt should show (after ball hits floor, before game over UI).
    /// </summary>
    public UnityEvent OnRevivePrompt = new UnityEvent();
    /// <summary>
    /// Fired when player accepts revive.
    /// </summary>
    public UnityEvent OnRevive = new UnityEvent();

    [Header("Settings")]
    [Tooltip("Base speed for hoop movement")]
    public float gameSpeed = 3f;

    [Tooltip("Maximum multiplier value (0 = no cap)")]
    [SerializeField] private int maxMultiplier = 16;

    // Score tracking
    private int score = 0;
    private int currentMultiplier = 1;

    // Game state
    private bool isPlaying = false;
    private bool hasStarted = false;

    // Cached player reference for shield checks
    private BallController playerBall;

    // Revive tracking - only one revive per game
    private bool hasUsedRevive = false;

    // Properties
    public bool IsPlaying => isPlaying;
    public bool HasStarted => hasStarted;
    public int Score => score;
    public int CurrentMultiplier => currentMultiplier;

    public int GetScore() => score;
    public int GetCurrentMultiplier() => currentMultiplier;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            EnforceSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Forces FPS cap and portrait orientation once on startup.
    /// </summary>
    void EnforceSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    /// <summary>
    /// Re-applies settings when app regains focus (e.g. after fullscreen toggle).
    /// </summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) EnforceSettings();
    }

    /// <summary>
    /// Starts a new game session.
    /// </summary>
    public void StartGame()
    {
        if (isPlaying) return;

        isPlaying = true;
        hasStarted = true;
        score = 0;
        currentMultiplier = 1;
        hasUsedRevive = false;
        Time.timeScale = 1f;

        OnGameStart.Invoke();
        OnScoreChanged.Invoke(score);
        OnMultiplierChanged.Invoke(currentMultiplier);

        Debug.Log("GameManager: Game started");
    }

    /// <summary>
    /// Ends the current game session.
    /// If the player has an active shield, the shield is consumed instead of dying.
    /// </summary>
    public void GameOver()
    {
        if (!isPlaying) return;

        // Shield or invincibility absorbs any death (hoop miss, boundary hit, etc.)
        if (playerBall == null)
            playerBall = Object.FindFirstObjectByType<BallController>();

        if (playerBall != null && (playerBall.IsInvincible || playerBall.UseShield()))
        {
            Debug.Log("GameManager: Shield/invincibility absorbed death!");
            return;
        }

        isPlaying = false;
        Time.timeScale = 1f;
        OnGameOver.Invoke();

        Debug.Log($"GameManager: Game over - Final Score: {score}");
    }

    /// <summary>
    /// Adds score when player passes through a hoop.
    /// </summary>
    /// <param name="basePoints">Base points for passing the hoop</param>
    /// <param name="wasCleanPass">True if player didn't touch hoop edges</param>
    public void AddScore(int basePoints, bool wasCleanPass)
    {
        if (!isPlaying) return;

        OnHoopPassed.Invoke(wasCleanPass);

        if (wasCleanPass)
        {
            // Clean pass: award points with current multiplier, then double it
            int pointsEarned = basePoints * currentMultiplier;
            score += pointsEarned;

            // Double the multiplier
            int oldMultiplier = currentMultiplier;
            currentMultiplier += 1;

            // Cap multiplier if maxMultiplier is set
            if (maxMultiplier > 0 && currentMultiplier > maxMultiplier)
            {
                currentMultiplier = maxMultiplier;
            }

            OnScoreChanged.Invoke(score);
            OnCleanPass.Invoke();

            if (currentMultiplier != oldMultiplier)
            {
                OnMultiplierChanged.Invoke(currentMultiplier);
            }

            Debug.Log($"GameManager: Clean pass! +{pointsEarned} points, multiplier now x{currentMultiplier}");
        }
        else
        {
            // Edge touch: award base points only, reset multiplier
            score += basePoints;

            if (currentMultiplier > 1)
            {
                currentMultiplier = 1;
                OnMultiplierChanged.Invoke(currentMultiplier);
                OnEdgeTouch.Invoke();
                Debug.Log($"GameManager: Edge touch - multiplier reset to x1");
            }

            OnScoreChanged.Invoke(score);
            Debug.Log($"GameManager: Edge touch pass. +{basePoints} points");
        }
    }

    /// <summary>
    /// Sets the multiplier to a specific value (e.g. ghost hoop x8 bonus).
    /// Only raises the multiplier - won't lower it if already higher.
    /// </summary>
    public void SetMultiplier(int value)
    {
        if (value <= currentMultiplier) return;

        if (maxMultiplier > 0 && value > maxMultiplier)
            value = maxMultiplier;

        currentMultiplier = value;
        OnMultiplierChanged.Invoke(currentMultiplier);
        Debug.Log($"GameManager: Multiplier set to x{currentMultiplier}");
    }

    /// <summary>
    /// Resets multiplier to 1 (called when touching edges without scoring).
    /// </summary>
    public void ResetMultiplier()
    {
        if (currentMultiplier > 1)
        {
            currentMultiplier = 1;
            OnMultiplierChanged.Invoke(currentMultiplier);
            OnEdgeTouch.Invoke();
        }
    }

    /// <summary>
    /// Restarts the game by reloading the scene.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Called by BallController when ball hits the floor after death.
    /// Shows revive prompt if available, otherwise shows game over UI.
    /// </summary>
    public void ShowGameOverUI()
    {
        if (!hasUsedRevive)
        {
            // Show revive prompt first
            OnRevivePrompt.Invoke();
            Debug.Log("GameManager: Showing revive prompt");
        }
        else
        {
            // Already used revive - go straight to game over
            OnShowGameOverUI.Invoke();
            Debug.Log("GameManager: Showing game over UI");
        }
    }

    /// <summary>
    /// Player accepts revive. Resumes game with shield active.
    /// </summary>
    public void AcceptRevive()
    {
        hasUsedRevive = true;
        isPlaying = true;

        if (playerBall == null)
            playerBall = Object.FindFirstObjectByType<BallController>();

        if (playerBall != null)
        {
            playerBall.Revive();
        }

        OnRevive.Invoke();
        Debug.Log("GameManager: Player revived with shield!");
    }

    /// <summary>
    /// Player declines revive. Shows game over UI.
    /// </summary>
    public void DeclineRevive()
    {
        OnShowGameOverUI.Invoke();
        Debug.Log("GameManager: Revive declined - showing game over UI");
    }
}
