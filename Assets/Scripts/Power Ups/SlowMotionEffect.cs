using UnityEngine;

/// <summary>
/// Effect component for slow motion power-up.
/// Manages time scale reduction and handles expiration.
/// </summary>
public class SlowMotionEffect : MonoBehaviour
{
    // Effect parameters
    private float duration;
    private float remainingTime;
    private float slowTimeScale;
    private Color effectColor;

    // State
    private bool isInitialized = false;
    private float originalFixedDeltaTime;

    /// <summary>
    /// Initializes the slow motion effect.
    /// </summary>
    /// <param name="effectDuration">How long the effect lasts</param>
    /// <param name="timeScale">The reduced time scale (e.g., 0.5 for half speed)</param>
    /// <param name="color">Color associated with this power-up</param>
    public void Initialize(float effectDuration, float timeScale, Color color)
    {
        duration = effectDuration;
        remainingTime = effectDuration;
        slowTimeScale = timeScale;
        effectColor = color;

        // Store original fixed delta time
        originalFixedDeltaTime = Time.fixedDeltaTime;

        // Apply slow motion
        ApplySlowMotion();

        isInitialized = true;
    }

    /// <summary>
    /// Applies the slow motion time scale.
    /// </summary>
    void ApplySlowMotion()
    {
        Time.timeScale = slowTimeScale;
        // Adjust fixed delta time to maintain physics consistency
        Time.fixedDeltaTime = originalFixedDeltaTime * slowTimeScale;
    }

    /// <summary>
    /// Unity Update - handles duration countdown using unscaled time.
    /// </summary>
    void Update()
    {
        if (!isInitialized) return;

        // Use unscaled delta time so countdown isn't affected by slow motion
        remainingTime -= Time.unscaledDeltaTime;

        // Check for expiration
        if (remainingTime <= 0)
        {
            Expire();
        }
    }

    /// <summary>
    /// Refreshes the effect duration (when collecting another slow motion power-up).
    /// </summary>
    public void RefreshDuration()
    {
        remainingTime = duration;
    }

    /// <summary>
    /// Called when the effect expires.
    /// </summary>
    void Expire()
    {
        // Restore normal time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        // Notify manager
        PowerUpManager.Instance.DeactivateSlowMotion();

        // Destroy this effect object
        Destroy(gameObject);
    }

    /// <summary>
    /// Unity OnDestroy - ensure time is restored if destroyed unexpectedly.
    /// </summary>
    void OnDestroy()
    {
        // Safety: restore time scale if still slowed
        if (Time.timeScale != 1f && isInitialized)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }

    /// <summary>
    /// Gets the remaining time on this effect.
    /// </summary>
    public float GetRemainingTime() => remainingTime;

    /// <summary>
    /// Gets the current slow motion scale.
    /// </summary>
    public float GetTimeScale() => slowTimeScale;
}
