using UnityEngine;

/// <summary>
/// Effect component for score multiplier power-up.
/// Applies a multiplier to all score gains and handles expiration.
/// </summary>
public class ScoreMultiplierEffect : MonoBehaviour
{
    // Effect parameters
    private float duration;
    private float remainingTime;
    private float multiplier;
    private Color effectColor;

    // State
    private bool isInitialized = false;

    /// <summary>
    /// Initializes the score multiplier effect.
    /// </summary>
    /// <param name="effectDuration">How long the effect lasts</param>
    /// <param name="scoreMultiplier">The multiplier value (e.g., 2 for double)</param>
    /// <param name="color">Color associated with this power-up</param>
    public void Initialize(float effectDuration, float scoreMultiplier, Color color)
    {
        duration = effectDuration;
        remainingTime = effectDuration;
        multiplier = scoreMultiplier;
        effectColor = color;

        // Apply multiplier to GameManager
        ApplyMultiplier();

        isInitialized = true;
    }

    /// <summary>
    /// Applies the score multiplier to GameManager.
    /// </summary>
    void ApplyMultiplier()
    {
        GameManager.Instance.SetScoreMultiplier(multiplier);
    }

    /// <summary>
    /// Unity Update - handles duration countdown.
    /// </summary>
    void Update()
    {
        if (!isInitialized) return;

        // Use unscaled delta time so slow motion doesn't affect duration
        remainingTime -= Time.unscaledDeltaTime;

        // Check for expiration
        if (remainingTime <= 0)
        {
            Expire();
        }
    }

    /// <summary>
    /// Refreshes the effect duration (when collecting another multiplier power-up).
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
        // Reset multiplier
        GameManager.Instance.ResetScoreMultiplier();

        // Notify manager
        PowerUpManager.Instance.DeactivateMultiplier();

        // Destroy this effect object
        Destroy(gameObject);
    }

    /// <summary>
    /// Unity OnDestroy - ensure multiplier is reset if destroyed unexpectedly.
    /// </summary>
    void OnDestroy()
    {
        if (isInitialized && GameManager.Instance != null)
        {
            GameManager.Instance.ResetScoreMultiplier();
        }
    }

    /// <summary>
    /// Gets the remaining time on this effect.
    /// </summary>
    public float GetRemainingTime() => remainingTime;

    /// <summary>
    /// Gets the current multiplier value.
    /// </summary>
    public float GetMultiplier() => multiplier;
}
