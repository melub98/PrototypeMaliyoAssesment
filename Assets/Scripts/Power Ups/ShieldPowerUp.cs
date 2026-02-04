using UnityEngine;

/// <summary>
/// Shield power-up collectible that grants temporary collision immunity.
/// When collected, creates a ShieldEffect that protects the player from one hit.
/// </summary>
public class ShieldPowerUp : PowerUpBase
{
    [Header("Shield Settings")]
    [Tooltip("Prefab for the shield visual effect around the ball")]
    [SerializeField] private GameObject shieldEffectPrefab;

    /// <summary>
    /// Activates the shield power-up when collected.
    /// </summary>
    protected override void Activate()
    {
        // Find the player ball
        SimpleBallController ball = FindFirstObjectByType<SimpleBallController>();
        if (ball == null) return;

        // Create shield effect attached to ball
        GameObject shieldObj;
        if (shieldEffectPrefab != null)
        {
            shieldObj = Instantiate(shieldEffectPrefab, ball.transform);
        }
        else
        {
            // Create a simple shield object if no prefab assigned
            shieldObj = new GameObject("ShieldEffect");
            shieldObj.transform.SetParent(ball.transform);
            shieldObj.transform.localPosition = Vector3.zero;
        }

        // Add or get ShieldEffect component
        ShieldEffect effect = shieldObj.GetComponent<ShieldEffect>();
        if (effect == null)
        {
            effect = shieldObj.AddComponent<ShieldEffect>();
        }

        // Initialize the effect
        effect.Initialize(duration, powerUpColor);

        // Register with PowerUpManager
        PowerUpManager.Instance.ActivateShield(effect);
    }
}
