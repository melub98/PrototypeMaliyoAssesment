using UnityEngine;

/// <summary>
/// Abstract base class for all power-up collectibles.
/// Handles trigger-based collection, movement, and provides hooks for activation effects.
/// Extend this class to create specific power-up types (Shield, SlowMotion, ScoreMultiplier).
///
/// SETUP REQUIREMENTS:
/// - Add a Collider2D component (CircleCollider2D recommended) with IsTrigger = true
/// - Add a SpriteRenderer for visual representation
/// - Tag the power-up prefab appropriately (optional)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class PowerUpBase : MonoBehaviour
{
    #region Serialized Fields

    [Header("Effect Settings")]
    [Tooltip("Duration of the power-up effect in seconds (0 for instant/permanent)")]
    [SerializeField] protected float duration = 5f;

    [Header("Movement")]
    [Tooltip("Movement speed (0 = use GameManager.gameSpeed)")]
    [SerializeField] protected float moveSpeed = 0f;

    [Tooltip("X position at which power-up is destroyed")]
    [SerializeField] protected float destroyXPosition = -12f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect to play on collection")]
    [SerializeField] protected ParticleSystem collectEffect;

    [Tooltip("Color tint for this power-up type")]
    [SerializeField] protected Color powerUpColor = Color.white;

    [Tooltip("Rotation speed for visual effect (degrees/second)")]
    [SerializeField] protected float rotationSpeed = 90f;

    [Tooltip("Bob animation amplitude")]
    [SerializeField] protected float bobAmplitude = 0.2f;

    [Tooltip("Bob animation speed")]
    [SerializeField] protected float bobSpeed = 2f;

    [Header("Audio")]
    [Tooltip("Sound to play on collection")]
    [SerializeField] protected AudioClip collectSound;

    [Tooltip("Volume for collect sound")]
    [Range(0f, 1f)]
    [SerializeField] protected float collectVolume = 1f;

    #endregion

    #region Protected Fields

    // Tracks if already collected to prevent double collection
    protected bool isCollected = false;
    // Reference to audio source for playing sounds
    protected AudioSource audioSource;
    // Reference to sprite renderer
    protected SpriteRenderer spriteRenderer;
    // Original Y position for bobbing animation
    protected float originalY;
    // Time tracker for animations
    protected float animationTime = 0f;
    // Actual move speed used
    protected float actualMoveSpeed;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity Awake - initializes components.
    /// </summary>
    protected virtual void Awake()
    {
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Set move speed
        if (moveSpeed > 0)
        {
            actualMoveSpeed = moveSpeed;
        }
        else if (GameManager.Instance != null)
        {
            actualMoveSpeed = GameManager.Instance.gameSpeed;
        }
        else
        {
            actualMoveSpeed = 3f;
        }

        // Store original position for bobbing
        originalY = transform.position.y;
    }

    /// <summary>
    /// Unity Start - subscribes to game events.
    /// </summary>
    protected virtual void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }

        // Apply color to sprite if available
        if (spriteRenderer != null)
        {
            spriteRenderer.color = powerUpColor;
        }
    }

    /// <summary>
    /// Unity Update - handles movement and animation.
    /// </summary>
    protected virtual void Update()
    {
        // Don't update if collected
        if (isCollected) return;

        // Check if game is playing
        bool isPlaying = GameManager.Instance != null && GameManager.Instance.IsPlaying;

        if (isPlaying)
        {
            // Move left with platforms
            transform.Translate(Vector3.left * actualMoveSpeed * Time.deltaTime, Space.World);

            // Update animation time
            animationTime += Time.deltaTime;

            // Apply bobbing animation
            ApplyBobAnimation();

            // Apply rotation
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

            // Destroy when off-screen
            if (transform.position.x < destroyXPosition)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Unity OnDestroy - cleanup.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }
    }

    #endregion

    #region Animation

    /// <summary>
    /// Applies bobbing animation to the power-up.
    /// </summary>
    protected virtual void ApplyBobAnimation()
    {
        if (bobAmplitude > 0)
        {
            float newY = originalY + Mathf.Sin(animationTime * bobSpeed) * bobAmplitude;
            Vector3 pos = transform.position;
            pos.y = newY;
            transform.position = pos;
        }
    }

    #endregion

    #region Collection

    /// <summary>
    /// Unity trigger callback - handles collection when player touches power-up.
    /// </summary>
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    /// <summary>
    /// Called when the power-up is collected by the player.
    /// Plays effects and activates the power-up.
    /// </summary>
    protected virtual void Collect()
    {
        isCollected = true;

        // Play collection effects
        PlayCollectEffects();

        // Activate the power-up effect
        Activate();

        // Destroy the collectible object
        Destroy(gameObject);
    }

    /// <summary>
    /// Plays visual and audio effects on collection.
    /// </summary>
    protected virtual void PlayCollectEffects()
    {
        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);
        }

        // Spawn particle effect if assigned
        if (collectEffect != null)
        {
            ParticleSystem effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            var main = effect.main;
            main.startColor = powerUpColor;
            effect.Play();
            Destroy(effect.gameObject, main.duration + main.startLifetime.constantMax);
        }
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Abstract method - must be implemented by derived classes to apply the power-up effect.
    /// </summary>
    protected abstract void Activate();

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when game ends.
    /// </summary>
    protected virtual void OnGameOver()
    {
        // Power-ups stop moving but remain visible
        actualMoveSpeed = 0;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the duration of this power-up.
    /// </summary>
    public float GetDuration() => duration;

    /// <summary>
    /// Gets the color associated with this power-up.
    /// </summary>
    public Color GetColor() => powerUpColor;

    #endregion
}
