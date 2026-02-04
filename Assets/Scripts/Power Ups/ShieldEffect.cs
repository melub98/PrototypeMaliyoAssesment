using UnityEngine;

/// <summary>
/// Visual and functional effect for the shield power-up.
/// Attached to the player ball, provides visual feedback and handles expiration.
/// Shield can be consumed by collision or expire after duration.
/// </summary>
public class ShieldEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Scale of the shield visual relative to ball")]
    [SerializeField] private float shieldScale = 1.5f;
    [Tooltip("Rotation speed of shield visual")]
    [SerializeField] private float rotationSpeed = 50f;
    [Tooltip("Pulse animation speed")]
    [SerializeField] private float pulseSpeed = 2f;
    [Tooltip("Pulse scale range")]
    [SerializeField] private float pulseAmount = 0.1f;

    [Header("Warning Settings")]
    [Tooltip("Time before expiry to start warning animation")]
    [SerializeField] private float warningTime = 2f;
    [Tooltip("Flash speed during warning")]
    [SerializeField] private float warningFlashSpeed = 10f;

    // Duration tracking
    private float duration;
    private float remainingTime;
    private Color shieldColor;

    // Visual components
    private SpriteRenderer spriteRenderer;
    private bool isInitialized = false;

    /// <summary>
    /// Initializes the shield effect with duration and color.
    /// </summary>
    /// <param name="effectDuration">How long the shield lasts</param>
    /// <param name="color">Color of the shield visual</param>
    public void Initialize(float effectDuration, Color color)
    {
        duration = effectDuration;
        remainingTime = effectDuration;
        shieldColor = color;

        // Set up visual
        SetupVisual();
        isInitialized = true;
    }

    /// <summary>
    /// Creates the shield visual if not already present.
    /// </summary>
    void SetupVisual()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            // Create a simple circle sprite programmatically or use default
            spriteRenderer.sprite = CreateCircleSprite();
        }

        spriteRenderer.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0.5f);
        spriteRenderer.sortingOrder = 10; // Render above ball
        transform.localScale = Vector3.one * shieldScale;
    }

    /// <summary>
    /// Creates a simple circle sprite for the shield visual.
    /// </summary>
    Sprite CreateCircleSprite()
    {
        // Create a simple 64x64 circle texture
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < radius && dist > radius - 4)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>
    /// Unity Update - handles animation and expiration.
    /// </summary>
    void Update()
    {
        if (!isInitialized) return;

        // Count down using unscaled time so slow motion doesn't affect duration
        remainingTime -= Time.unscaledDeltaTime;

        // Animate shield
        AnimateShield();

        // Check for expiration
        if (remainingTime <= 0)
        {
            Expire();
        }
    }

    /// <summary>
    /// Animates the shield visual with rotation and pulse.
    /// </summary>
    void AnimateShield()
    {
        // Rotate
        transform.Rotate(Vector3.forward * rotationSpeed * Time.unscaledDeltaTime);

        // Pulse scale
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        transform.localScale = Vector3.one * shieldScale * pulse;

        // Warning flash when about to expire
        if (remainingTime <= warningTime && spriteRenderer != null)
        {
            float flash = Mathf.Abs(Mathf.Sin(Time.unscaledTime * warningFlashSpeed));
            spriteRenderer.color = new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0.3f + flash * 0.5f);
        }
    }

    /// <summary>
    /// Refreshes the shield duration (when collecting another shield).
    /// </summary>
    public void RefreshDuration()
    {
        remainingTime = duration;
    }

    /// <summary>
    /// Called when the shield absorbs a hit.
    /// </summary>
    public void OnConsumed()
    {
        // Could play a break effect here
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when the shield expires naturally.
    /// </summary>
    void Expire()
    {
        PowerUpManager.Instance.DeactivateShield();
        Destroy(gameObject);
    }
}
