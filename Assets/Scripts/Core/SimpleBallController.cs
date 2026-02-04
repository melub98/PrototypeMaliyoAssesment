using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the player's ball movement and physics.
/// Features simple jump mechanics, first-tap-to-start, shield integration,
/// and mobile touch support using the new Input System.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SimpleBallController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Movement")]
    [Tooltip("Upward force applied when player taps/clicks")]
    [SerializeField] private float jumpForce = 6f;

    [Tooltip("Gravity multiplier for snappier falling")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Rotation")]
    [Tooltip("How quickly the ball rotates based on vertical velocity")]
    [SerializeField] private float rotationSpeed = 5f;

    [Tooltip("Maximum rotation angle in degrees")]
    [SerializeField] private float maxRotation = 45f;

    [Header("Boundaries")]
    [Tooltip("Maximum Y position (ceiling)")]
    [SerializeField] private float maxY = 4.5f;

    [Tooltip("Minimum Y position (floor)")]
    [SerializeField] private float minY = -4.5f;

    [Header("Audio")]
    [Tooltip("Sound played when jumping")]
    [SerializeField] private AudioClip jumpSound;

    [Tooltip("Sound played when dying")]
    [SerializeField] private AudioClip deathSound;

    [Tooltip("Sound played when shield blocks a hit")]
    [SerializeField] private AudioClip shieldBlockSound;

    [Header("Visual Feedback")]
    [Tooltip("Reference to squash/stretch component (optional)")]
    [SerializeField] private BallSquashStretch squashStretch;

    #endregion

    #region Private Fields

    // Component references
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;

    // Input System references
    private InputAction jumpAction;
    private InputAction touchAction;

    // State tracking
    private bool isDead = false;
    private bool hasShield = false;
    private Vector3 startPosition;
    private Color originalColor;
    private bool jumpPressed = false;

    #endregion

    #region Properties

    /// <summary>
    /// Check/set shield status. When true, next collision is absorbed.
    /// </summary>
    public bool HasShield
    {
        get => hasShield;
        set
        {
            hasShield = value;
            UpdateShieldVisual();
        }
    }

    /// <summary>
    /// Check if player is dead.
    /// </summary>
    public bool IsDead => isDead;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // Cache component references
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Add AudioSource if not present
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Store initial values
        startPosition = transform.position;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Get squash stretch if not assigned
        if (squashStretch == null)
        {
            squashStretch = GetComponent<BallSquashStretch>();
        }

        // Setup Input System actions
        SetupInputActions();
    }

    void Start()
    {
        // Initialize physics - ball hovers until game starts
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }
    }

    void OnEnable()
    {
        // Enable input actions
        jumpAction?.Enable();
        touchAction?.Enable();
    }

    void OnDisable()
    {
        // Disable input actions
        jumpAction?.Disable();
        touchAction?.Disable();
    }

    void Update()
    {
        // Check for jump input
        if (jumpPressed)
        {
            jumpPressed = false;
            HandleInput();
        }

        // Apply rotation and boundary checks when playing
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying && !isDead)
        {
            HandleRotation();
            ClampPosition();
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || isDead) return;

        // Apply extra gravity when falling for snappier feel
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }

        // Dispose input actions
        jumpAction?.Dispose();
        touchAction?.Dispose();
    }

    #endregion

    #region Input Setup

    /// <summary>
    /// Sets up Input System actions for keyboard, mouse, and touch.
    /// </summary>
    void SetupInputActions()
    {
        // Create jump action for spacebar and gamepad
        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.performed += OnJumpPerformed;

        // Create touch/click action
        touchAction = new InputAction("Touch", binding: "<Mouse>/leftButton");
        touchAction.AddBinding("<Touchscreen>/primaryTouch/tap");
        touchAction.performed += OnJumpPerformed;

        // Enable actions
        jumpAction.Enable();
        touchAction.Enable();
    }

    /// <summary>
    /// Called when jump input is performed.
    /// </summary>
    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Handles tap/click input - starts game on first tap, then jumps.
    /// </summary>
    void HandleInput()
    {
        // Don't process input if dead
        if (isDead) return;

        // First tap starts the game
        if (GameManager.Instance != null && !GameManager.Instance.HasStarted)
        {
            GameManager.Instance.StartGame();
        }

        // Only jump if game is playing
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            Jump();
        }
    }

    #endregion

    #region Movement

    /// <summary>
    /// Applies upward force for jumping.
    /// </summary>
    void Jump()
    {
        // Reset vertical velocity and apply jump force
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        // Play jump sound
        PlaySound(jumpSound);

        // Trigger squash effect
        if (squashStretch != null)
        {
            squashStretch.TriggerImpact();
        }
    }

    /// <summary>
    /// Rotates the ball based on vertical velocity for visual feedback.
    /// </summary>
    void HandleRotation()
    {
        float targetRotation = Mathf.Clamp(rb.linearVelocity.y * rotationSpeed, -maxRotation, maxRotation);
        transform.rotation = Quaternion.Euler(0, 0, targetRotation);
    }

    /// <summary>
    /// Keeps the ball within vertical boundaries.
    /// </summary>
    void ClampPosition()
    {
        Vector3 pos = transform.position;

        // Check ceiling - clamp position
        if (pos.y > maxY)
        {
            pos.y = maxY;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            transform.position = pos;
        }
        // Check floor - triggers game over
        else if (pos.y < minY)
        {
            HandleDeath();
        }
    }

    #endregion

    #region Game State

    /// <summary>
    /// Called when game starts - enables physics and resets state.
    /// </summary>
    void OnGameStart()
    {
        rb.gravityScale = 1;
        isDead = false;
        hasShield = false;
        UpdateShieldVisual();

        // Reset position if needed
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Called when game ends - stops movement.
    /// </summary>
    void OnGameOver()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
    }

    #endregion

    #region Collision Handling

    /// <summary>
    /// Unity collision callback - handles platform and boundary collisions.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Ignore collisions if dead or game not playing
        if (isDead) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // Check for deadly collision using tags
        if (collision.gameObject.CompareTag("Platform") ||
            collision.gameObject.CompareTag("Obstacle") ||
            collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Ceiling"))
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// Handles death logic with shield check.
    /// </summary>
    void HandleDeath()
    {
        // If shield is active, consume it instead of dying
        if (hasShield)
        {
            hasShield = false;
            UpdateShieldVisual();

            // Play shield block sound
            PlaySound(shieldBlockSound);

            // Notify PowerUpManager that shield was consumed
            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.OnShieldConsumed();
            }

            // Small bounce back effect
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.5f);
            return;
        }

        // No shield - trigger game over
        PlaySound(deathSound);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    #endregion

    #region Visual & Audio

    /// <summary>
    /// Updates visual feedback for shield status.
    /// </summary>
    void UpdateShieldVisual()
    {
        // Visual indication that shield is active (optional tint)
        if (spriteRenderer != null)
        {
            if (hasShield)
            {
                // Add slight blue tint when shielded
                spriteRenderer.color = new Color(
                    originalColor.r * 0.8f,
                    originalColor.g * 0.8f,
                    originalColor.b + 0.2f,
                    originalColor.a
                );
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    /// <summary>
    /// Plays a sound effect if audio clip is assigned.
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Resets the ball to its initial state.
    /// </summary>
    public void ResetBall()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        isDead = false;
        hasShield = false;
        UpdateShieldVisual();
    }

    #endregion
}
