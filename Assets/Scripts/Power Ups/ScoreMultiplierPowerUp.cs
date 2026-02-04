using UnityEngine;

/// <summary>
/// Score multiplier power-up collectible that increases score gained.
/// When collected, creates a ScoreMultiplierEffect that multiplies all score gains.
/// </summary>
public class ScoreMultiplierPowerUp : PowerUpBase
{
    [Header("Multiplier Settings")]
    [Tooltip("Score multiplier when active (2 = double points)")]
    [SerializeField] private float scoreMultiplier = 2f;

    /// <summary>
    /// Activates the score multiplier power-up when collected.
    /// </summary>
    protected override void Activate()
    {
        // Create effect object
        GameObject effectObj = new GameObject("ScoreMultiplierEffect");

        // Add ScoreMultiplierEffect component
        ScoreMultiplierEffect effect = effectObj.AddComponent<ScoreMultiplierEffect>();

        // Initialize the effect
        effect.Initialize(duration, scoreMultiplier, powerUpColor);

        // Register with PowerUpManager
        PowerUpManager.Instance.ActivateMultiplier(effect);
    }
}
