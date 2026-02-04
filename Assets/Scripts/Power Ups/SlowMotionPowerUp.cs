using UnityEngine;

/// <summary>
/// Slow motion power-up collectible that reduces game speed temporarily.
/// When collected, creates a SlowMotionEffect that slows down time.
/// </summary>
public class SlowMotionPowerUp : PowerUpBase
{
    [Header("Slow Motion Settings")]
    [Tooltip("Time scale when active (0.5 = half speed)")]
    [SerializeField] private float slowTimeScale = 0.5f;

    /// <summary>
    /// Activates the slow motion power-up when collected.
    /// </summary>
    protected override void Activate()
    {
        // Create effect object
        GameObject effectObj = new GameObject("SlowMotionEffect");

        // Add SlowMotionEffect component
        SlowMotionEffect effect = effectObj.AddComponent<SlowMotionEffect>();

        // Initialize the effect
        effect.Initialize(duration, slowTimeScale, powerUpColor);

        // Register with PowerUpManager
        PowerUpManager.Instance.ActivateSlowMotion(effect);
    }
}
