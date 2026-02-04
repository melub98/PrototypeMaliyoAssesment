using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton manager for coordinating all active power-up effects.
/// Handles activation, expiration, and state tracking for all power-ups.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    // Singleton instance
    public static PowerUpManager Instance { get; private set; }

    [Header("Events")]
    // Event fired when shield is activated
    public UnityEvent OnShieldActivated = new UnityEvent();
    // Event fired when shield expires or is consumed
    public UnityEvent OnShieldDeactivated = new UnityEvent();
    // Event fired when slow motion is activated
    public UnityEvent OnSlowMotionActivated = new UnityEvent();
    // Event fired when slow motion expires
    public UnityEvent OnSlowMotionDeactivated = new UnityEvent();
    // Event fired when score multiplier is activated
    public UnityEvent OnMultiplierActivated = new UnityEvent();
    // Event fired when score multiplier expires
    public UnityEvent OnMultiplierDeactivated = new UnityEvent();

    [Header("Active Effect References")]
    [Tooltip("Reference to the active shield effect (if any)")]
    [SerializeField] private ShieldEffect activeShield;
    [Tooltip("Reference to the active slow motion effect (if any)")]
    [SerializeField] private SlowMotionEffect activeSlowMotion;
    [Tooltip("Reference to the active score multiplier effect (if any)")]
    [SerializeField] private ScoreMultiplierEffect activeMultiplier;

    // State tracking
    private bool isShieldActive = false;
    private bool isSlowMotionActive = false;
    private bool isMultiplierActive = false;

    // Public accessors for power-up states
    public bool IsShieldActive => isShieldActive;
    public bool IsSlowMotionActive => isSlowMotionActive;
    public bool IsMultiplierActive => isMultiplierActive;

    /// <summary>
    /// Unity Awake - sets up singleton.
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Unity Start - subscribes to game events.
    /// </summary>
    void Start()
    {
        GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        GameManager.Instance.OnGameStart.AddListener(OnGameStart);
    }

    /// <summary>
    /// Called when game starts - resets all power-up states.
    /// </summary>
    void OnGameStart()
    {
        DeactivateAllPowerUps();
    }

    /// <summary>
    /// Called when game ends - cleans up all active power-ups.
    /// </summary>
    void OnGameOver()
    {
        DeactivateAllPowerUps();
    }

    /// <summary>
    /// Deactivates all currently active power-ups.
    /// </summary>
    public void DeactivateAllPowerUps()
    {
        if (isShieldActive) DeactivateShield();
        if (isSlowMotionActive) DeactivateSlowMotion();
        if (isMultiplierActive) DeactivateMultiplier();
    }

    #region Shield Power-Up

    /// <summary>
    /// Activates the shield power-up.
    /// </summary>
    /// <param name="effect">The shield effect component</param>
    public void ActivateShield(ShieldEffect effect)
    {
        // If shield already active, refresh it
        if (isShieldActive && activeShield != null)
        {
            activeShield.RefreshDuration();
            return;
        }

        activeShield = effect;
        isShieldActive = true;

        // Enable shield on ball
        SimpleBallController ball = FindFirstObjectByType<SimpleBallController>();
        if (ball != null)
        {
            ball.HasShield = true;
        }

        OnShieldActivated.Invoke();
    }

    /// <summary>
    /// Deactivates the shield power-up.
    /// </summary>
    public void DeactivateShield()
    {
        isShieldActive = false;
        activeShield = null;

        // Disable shield on ball
        SimpleBallController ball = FindFirstObjectByType<SimpleBallController>();
        if (ball != null)
        {
            ball.HasShield = false;
        }

        OnShieldDeactivated.Invoke();
    }

    /// <summary>
    /// Called when shield absorbs a hit.
    /// </summary>
    public void OnShieldConsumed()
    {
        if (activeShield != null)
        {
            activeShield.OnConsumed();
        }
        DeactivateShield();
    }

    #endregion

    #region Slow Motion Power-Up

    /// <summary>
    /// Activates the slow motion power-up.
    /// </summary>
    /// <param name="effect">The slow motion effect component</param>
    public void ActivateSlowMotion(SlowMotionEffect effect)
    {
        // If already active, refresh duration
        if (isSlowMotionActive && activeSlowMotion != null)
        {
            activeSlowMotion.RefreshDuration();
            return;
        }

        activeSlowMotion = effect;
        isSlowMotionActive = true;
        OnSlowMotionActivated.Invoke();
    }

    /// <summary>
    /// Deactivates the slow motion power-up.
    /// </summary>
    public void DeactivateSlowMotion()
    {
        isSlowMotionActive = false;
        activeSlowMotion = null;
        Time.timeScale = 1f; // Restore normal time
        OnSlowMotionDeactivated.Invoke();
    }

    #endregion

    #region Score Multiplier Power-Up

    /// <summary>
    /// Activates the score multiplier power-up.
    /// </summary>
    /// <param name="effect">The multiplier effect component</param>
    public void ActivateMultiplier(ScoreMultiplierEffect effect)
    {
        // If already active, refresh duration
        if (isMultiplierActive && activeMultiplier != null)
        {
            activeMultiplier.RefreshDuration();
            return;
        }

        activeMultiplier = effect;
        isMultiplierActive = true;
        OnMultiplierActivated.Invoke();
    }

    /// <summary>
    /// Deactivates the score multiplier power-up.
    /// </summary>
    public void DeactivateMultiplier()
    {
        isMultiplierActive = false;
        activeMultiplier = null;
        GameManager.Instance.ResetScoreMultiplier();
        OnMultiplierDeactivated.Invoke();
    }

    #endregion
}
