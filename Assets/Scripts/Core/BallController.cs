using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Controls the player's ball in Flappy Jump.
///
/// DESIGN DECISIONS:
/// - Ball maintains a fixed X position while the world scrolls past (classic Flappy Bird style)
/// - Uses Unity's new Input System for cross-platform input (keyboard, touch, gamepad)
/// - Implements a shield system that absorbs one boundary hit before game over
/// - Ball rotates based on velocity for visual feedback of movement direction
/// - Uses extra gravity when falling for snappier, more responsive controls
///
/// ARCHITECTURE:
/// - Subscribes to GameManager events for game state changes
/// - Decoupled from other systems through event-driven communication
/// - Shield visual is a separate GameObject for easy customization
///
/// COLOR CHANGE FEATURE:
/// - Ball changes color on clean passes to match the multiplier
/// - Colors progress from white -> yellow -> orange -> red -> purple as multiplier increases
/// - This provides immediate visual feedback of the player's streak
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Movement")]
    [Tooltip("Upward force applied when player taps/clicks. Higher = bigger jumps")]
    [SerializeField] private float jumpForce = 6f;

    [Tooltip("Extra gravity multiplier when falling. Makes descent feel snappier than ascent")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Position Lock")]
    [Tooltip("X position where ball stays fixed. World scrolls past while ball stays here")]
    [SerializeField] private float lockedXPosition = -3f;

    [Header("Rotation")]
    [Tooltip("Max upward tilt angle when rising (degrees)")]
    [SerializeField] private float maxUpAngle = 30f;

    [Tooltip("Max downward tilt angle when falling (degrees)")]
    [SerializeField] private float maxDownAngle = -60f;

    [Tooltip("How quickly the tilt responds to velocity changes")]
    [SerializeField] private float tiltSpeed = 5f;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip shieldBlockSound;

    [Header("Visual")]
    [Tooltip("GameObject shown around ball when shield is active")]
    [SerializeField] private GameObject shieldVisual;

    [Header("Multiplier Visual Effects")]
    [Tooltip("Enable color change when multiplier increases")]
    [SerializeField] private bool enableColorChange = true;

    [Tooltip("Default ball color (no multiplier)")]
    [SerializeField] private Color defaultColor = Color.white;

    [Tooltip("Color at 2x multiplier")]
    [SerializeField] private Color color2x = Color.yellow;

    [Tooltip("Color at 4x multiplier")]
    [SerializeField] private Color color4x = new Color(1f, 0.5f, 0f); // Orange

    [Tooltip("Color at 8x multiplier")]
    [SerializeField] private Color color8x = Color.red;

    [Tooltip("Color at 16x multiplier")]
    [SerializeField] private Color color16x = new Color(0.8f, 0f, 1f); // Purple

    [Tooltip("How fast to transition between colors")]
    [SerializeField] private float colorTransitionSpeed = 5f;

    [Header("Fire/Trail Effect")]
    [Tooltip("Particle system for fire/trail effect when on a streak")]
    [SerializeField] private ParticleSystem fireEffect;

    [Tooltip("Minimum multiplier to show fire effect")]
    [SerializeField] private int fireEffectThreshold = 4;

    [Tooltip("Base emission rate for fire")]
    [SerializeField] private float baseFireEmission = 15f;

    [Tooltip("Max emission rate at max multiplier")]
    [SerializeField] private float maxFireEmission = 60f;

    #endregion

    #region Private Fields

    // Component references cached for performance
    private Rigidbody2D rb;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;

    // Input actions for cross-platform input
    private InputAction jumpAction;
    private InputAction touchAction;

    // State tracking
    private bool isDead = false;
    private bool hasShield = false;
    private Vector3 startPosition;
    private bool jumpPressed = false;

    // Death sequence tracking - UI shows after ball hits floor
    private bool hasHitFloorAfterDeath = false;

    // Color change tracking
    private Color targetColor;
    private Color currentColor;

    // Fire effect tracking
    private ParticleSystem.EmissionModule fireEmission;
    private bool fireEffectActive = false;
    private int currentMultiplier = 1;

    #endregion

    #region Properties

    /// <summary>
    /// Whether the ball currently has a shield.
    /// Setting this property automatically updates the shield visual.
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
    /// Whether the ball is dead (game over state).
    /// Used by other systems to check if ball should respond to events.
    /// </summary>
    public bool IsDead => isDead;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity Awake - Initialize components and input before Start.
    ///
    /// DECISION: Cache component references in Awake for better performance.
    /// GetComponent calls are expensive, so we do them once and store results.
    /// </summary>
    void Awake()
    {
        // Cache required components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // Add AudioSource if missing - allows prefab to work without manual setup
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Store start position for reset functionality
        startPosition = transform.position;
        lockedXPosition = startPosition.x;

        // Initialize colors
        currentColor = defaultColor;
        targetColor = defaultColor;

        // Setup fire effect if assigned
        if (fireEffect != null)
        {
            fireEmission = fireEffect.emission;
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
        }

        SetupInputActions();
    }

    /// <summary>
    /// Unity Start - Subscribe to events and set initial state.
    ///
    /// DECISION: Gravity starts at 0 so ball floats until game begins.
    /// This creates a "tap to start" feel where ball waits for input.
    ///
    /// </summary>
    void Start()
    {
        // Ball should float in place until game starts
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.freezeRotation = false;

        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
            GameManager.Instance.OnMultiplierChanged.AddListener(OnMultiplierChanged);
            GameManager.Instance.OnCleanPass.AddListener(OnCleanPass);
        }

        UpdateShieldVisual();

        // Apply initial color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = currentColor;
        }
    }

    /// <summary>
    /// Enable input actions when object becomes active.
    /// </summary>
    void OnEnable()
    {
        jumpAction?.Enable();
        touchAction?.Enable();
    }

    /// <summary>
    /// Disable input actions when object becomes inactive.
    /// Prevents input processing when ball isn't in scene.
    /// </summary>
    void OnDisable()
    {
        jumpAction?.Disable();
        touchAction?.Disable();
    }

    /// <summary>
    /// Unity Update - Process input and visual updates.
    ///
    /// DECISION: Input is buffered (jumpPressed flag) then processed in Update.
    /// This ensures consistent input handling regardless of frame rate.
    ///
    /// </summary>
    void Update()
    {
        // Process buffered input
        if (jumpPressed)
        {
            jumpPressed = false;
            HandleInput();
        }

        // Only update during gameplay (not dead)
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying && !isDead)
        {
            LockHorizontalPosition();
            UpdateTilt();
        }

        // Smooth color transition
        if (enableColorChange && spriteRenderer != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
            spriteRenderer.color = currentColor;
        }
    }

    /// <summary>
    /// Unity FixedUpdate - Physics calculations at fixed timestep.
    ///
    /// DECISION: Extra gravity when falling creates asymmetric jump feel.
    /// Rising is floaty (normal gravity), falling is snappy (extra gravity).
    /// This makes controls feel more responsive and game more challenging.
    /// </summary>
    void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || isDead) return;

        // Apply extra gravity when falling for snappier controls
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Unity OnDestroy - Clean up event subscriptions.
    ///
    /// IMPORTANT: Always unsubscribe from events to prevent memory leaks
    /// and errors when GameManager is destroyed before this object.
    /// </summary>
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
            GameManager.Instance.OnMultiplierChanged.RemoveListener(OnMultiplierChanged);
            GameManager.Instance.OnCleanPass.RemoveListener(OnCleanPass);
        }

        // Dispose input actions to prevent memory leaks
        jumpAction?.Dispose();
        touchAction?.Dispose();
    }

    #endregion

    #region Input

    /// <summary>
    /// Sets up input actions for keyboard, mouse, touch, and gamepad.
    ///
    /// DECISION: Using Unity's new Input System for several reasons:
    /// 1. Cross-platform support (works on mobile, PC, console)
    /// 2. Better performance than old Input.GetKeyDown checks
    /// 3. Supports multiple input devices simultaneously
    /// </summary>
    void SetupInputActions()
    {
        // Keyboard/Gamepad jump
        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth"); // A button on Xbox, X on PlayStation
        jumpAction.performed += OnJumpPerformed;

        // Mouse/Touch input
        touchAction = new InputAction("Touch", binding: "<Mouse>/leftButton");
        touchAction.AddBinding("<Touchscreen>/primaryTouch/tap");
        touchAction.performed += OnTouchPerformed;

        jumpAction.Enable();
        touchAction.Enable();
    }

    /// <summary>
    /// Callback when jump input is detected (keyboard/gamepad).
    /// Sets a flag that's processed in Update for consistent timing.
    /// </summary>
    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    /// <summary>
    /// Callback when touch/click input is detected.
    /// Ignores input if clicking on UI elements (buttons, etc).
    /// </summary>
    void OnTouchPerformed(InputAction.CallbackContext context)
    {
        // Don't jump if clicking on UI
        if (IsPointerOverUI()) return;
        jumpPressed = true;
    }

    /// <summary>
    /// Checks if the pointer (mouse or touch) is over a UI element.
    ///
    /// DECISION: This prevents accidental jumps when using UI buttons.
    /// Uses Unity's EventSystem raycasting for accurate UI detection.
    /// </summary>
    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Check mouse position
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = mousePos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            if (results.Count > 0) return true;
        }

        // Check touch position
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = touchPos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            if (results.Count > 0) return true;
        }

        return false;
    }

    /// <summary>
    /// Processes input based on current game state.
    /// Only allows jumping during active gameplay.
    /// </summary>
    void HandleInput()
    {
        if (isDead) return;

        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            Jump();
        }
    }

    #endregion

    #region Movement

    /// <summary>
    /// Applies upward force for jump.
    ///
    /// DECISION: Completely replaces Y velocity instead of adding to it.
    /// This ensures consistent jump height regardless of current velocity.
    /// </summary>
    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        PlaySound(jumpSound);
    }

    /// <summary>
    /// Tilts the ball based on Y velocity — up when rising, down when falling.
    /// </summary>
    void UpdateTilt()
    {
        // Map velocity to target angle: positive velocity = tilt up, negative = tilt down
        float targetAngle;
        if (rb.linearVelocity.y > 0)
        {
            targetAngle = Mathf.Lerp(0f, maxUpAngle, rb.linearVelocity.y / jumpForce);
        }
        else
        {
            targetAngle = Mathf.Lerp(0f, maxDownAngle, -rb.linearVelocity.y / (jumpForce * 2f));
        }

        targetAngle = Mathf.Clamp(targetAngle, maxDownAngle, maxUpAngle);

        // Drive rotation via angular velocity so it works with physics
        float currentAngle = rb.rotation;
        if (currentAngle > 180f) currentAngle -= 360f;
        float angleDiff = targetAngle - currentAngle;
        rb.angularVelocity = angleDiff * tiltSpeed;
    }

    /// <summary>
    /// Keeps ball at fixed X position while world scrolls past.
    ///
    /// DESIGN PATTERN: Classic Flappy Bird approach where player stays still
    /// and obstacles move. This simplifies collision detection and camera work.
    ///
    /// IMPLEMENTATION: Snaps position if drifted more than 0.01 units.
    /// Small threshold prevents micro-corrections every frame.
    /// </summary>
    void LockHorizontalPosition()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x - lockedXPosition) > 0.01f)
        {
            pos.x = lockedXPosition;
            transform.position = pos;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    #endregion

    #region Game State

    /// <summary>
    /// Called when game starts.
    /// Resets ball to initial state and performs first jump.
    /// </summary>
    void OnGameStart()
    {
        // Enable physics
        rb.gravityScale = 1;
        isDead = false;
        hasShield = false;
        hasHitFloorAfterDeath = false;
        UpdateShieldVisual();

        // Reset position and rotation
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = false;

        // Reset color to default
        targetColor = defaultColor;
        currentColor = defaultColor;
        currentMultiplier = 1;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = currentColor;
        }

        // Reset fire effect
        if (fireEffect != null)
        {
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
            fireEffectActive = false;
        }

        // Initial jump to start gameplay
        Jump();
    }

    /// <summary>
    /// Called when game ends.
    ///
    /// DESIGN DECISION: Ball falls to the ground on game over.
    /// The game over UI only appears AFTER ball hits the floor.
    /// This provides visual finality to the game ending and feels satisfying.
    ///
    /// DEATH SEQUENCE:
    /// 1. isDead = true, stops gameplay input
    /// 2. Ball gets slight upward pop then falls with gravity
    /// 3. Ball can bounce/roll naturally on floor (physics enabled)
    /// 4. When ball hits floor, OnShowGameOverUI is triggered
    /// </summary>
    void OnGameOver()
    {
        isDead = true;
        hasHitFloorAfterDeath = false;

        // IMPORTANT: Enable gravity so ball falls to ground
        // Ball keeps its current velocity and continues its trajectory naturally
        rb.gravityScale = 1.5f; // Slightly higher for dramatic fall

        // Unfreeze rotation for physics-driven tumble on death
        rb.freezeRotation = false;
        rb.angularVelocity = Random.Range(-180f, 180f);

        Debug.Log("BallController: Death sequence started - ball will fall to floor");
    }

    /// <summary>
    /// Called when multiplier changes.
    /// Updates ball color and fire effect to match the new multiplier level.
    ///
    /// DESIGN DECISION: Visual feedback for multiplier streaks.
    /// Players can see their streak status at a glance by ball color and fire trail.
    /// </summary>
    void OnMultiplierChanged(int multiplier)
    {
        currentMultiplier = multiplier;

        // Set target color based on multiplier
        if (enableColorChange)
        {
            targetColor = GetColorForMultiplier(multiplier);
        }

        // Update fire effect
        UpdateFireEffect(multiplier);
    }

    /// <summary>
    /// Updates the fire effect based on current multiplier.
    /// Fire activates at threshold (4x) and intensifies with higher multipliers.
    /// </summary>
    void UpdateFireEffect(int multiplier)
    {
        if (fireEffect == null) return;

        bool shouldShowFire = multiplier >= fireEffectThreshold;

        if (shouldShowFire && !fireEffectActive)
        {
            // Activate fire effect
            fireEffect.gameObject.SetActive(true);
            fireEffect.Play();
            fireEffectActive = true;
        }
        else if (!shouldShowFire && fireEffectActive)
        {
            // Deactivate fire effect
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
            fireEffectActive = false;
        }

        // Scale fire intensity with multiplier
        if (shouldShowFire)
        {
            float intensity = Mathf.Lerp(baseFireEmission, maxFireEmission,
                (multiplier - fireEffectThreshold) / 12f);
            fireEmission.rateOverTime = intensity;

            // Update fire color to match ball color
            var main = fireEffect.main;
            main.startColor = GetColorForMultiplier(multiplier);
        }
    }

    /// <summary>
    /// Called on clean pass (no edge touch).
    /// Could trigger additional visual effects here.
    /// </summary>
    void OnCleanPass()
    {
        // Ball color is already updated via OnMultiplierChanged
        // This method can be used for additional clean pass effects
    }

    /// <summary>
    /// Returns the appropriate color for a given multiplier value.
    /// Colors progress through a rainbow as multiplier increases.
    /// </summary>
    Color GetColorForMultiplier(int multiplier)
    {
        switch (multiplier)
        {
            case 1: return defaultColor;
            case 2: return color2x;
            case 4: return color4x;
            case 8: return color8x;
            default: return color16x; // 16x and above
        }
    }

    #endregion

    #region Collision

    /// <summary>
    /// Handles collision with other objects.
    ///
    /// DURING GAMEPLAY: Boundary collisions trigger game over (unless shielded).
    /// AFTER DEATH: Boundary collision (floor) triggers the game over UI to appear.
    /// Hoop edge collisions are handled separately in HoopEdgeCollider.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        // After death, detect floor hit to show game over UI
        if (isDead)
        {
            if (collision.gameObject.CompareTag("Boundary") && !hasHitFloorAfterDeath)
            {
                hasHitFloorAfterDeath = true;
                // Ball hit the floor after death - now show game over UI
                GameManager.Instance?.ShowGameOverUI();
                Debug.Log("BallController: Hit floor after death - showing game over UI");
            }
            return; // Don't process other collisions when dead
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (collision.gameObject.CompareTag("Boundary"))
        {
            HandleBoundaryHit();
        }
    }

    /// <summary>
    /// Catches the case where the ball is already on the floor when death occurs.
    /// OnCollisionEnter2D won't re-fire for an ongoing collision, but Stay will.
    /// </summary>
    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead && !hasHitFloorAfterDeath && collision.gameObject.CompareTag("Boundary"))
        {
            hasHitFloorAfterDeath = true;
            GameManager.Instance?.ShowGameOverUI();
        }
    }

    /// <summary>
    /// Processes hitting a boundary (ceiling or floor).
    ///
    /// SHIELD MECHANIC: If player has shield, it absorbs the hit.
    /// Shield is consumed, player bounces slightly, and continues playing.
    /// Without shield, hitting boundary ends the game.
    /// </summary>
    void HandleBoundaryHit()
    {
        if (hasShield)
        {
            // Shield absorbs the hit
            hasShield = false;
            UpdateShieldVisual();
            PlaySound(shieldBlockSound);

            // Bounce away from boundary
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.5f);
            Debug.Log("BallController: Shield absorbed boundary hit");
            return;
        }

        // No shield - game over
        PlaySound(deathSound);
        GameManager.Instance?.GameOver();
    }

    #endregion

    #region Shield

    /// <summary>
    /// Grants a shield to the player.
    /// Called when player collects a shield power-up.
    /// </summary>
    public void GrantShield()
    {
        hasShield = true;
        UpdateShieldVisual();
        Debug.Log("BallController: Shield granted");
    }

    /// <summary>
    /// Attempts to use the shield.
    /// Returns true if shield was available and used.
    /// </summary>
    public bool UseShield()
    {
        if (hasShield)
        {
            hasShield = false;
            UpdateShieldVisual();
            PlaySound(shieldBlockSound);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates the shield visual to match current shield state.
    /// Shield visual is a separate GameObject for easy customization.
    /// </summary>
    void UpdateShieldVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(hasShield);
        }
    }

    #endregion

    #region Audio

    /// <summary>
    /// Plays an audio clip if available.
    /// Uses PlayOneShot to allow overlapping sounds.
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
    /// Resets ball to initial state.
    /// Used when restarting game without reloading scene.
    /// </summary>
    public void ResetBall()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0;
        rb.freezeRotation = false;
        isDead = false;
        hasShield = false;
        hasHitFloorAfterDeath = false;
        UpdateShieldVisual();

        // Reset colors
        targetColor = defaultColor;
        currentColor = defaultColor;
        currentMultiplier = 1;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = currentColor;
        }

        // Reset fire effect
        if (fireEffect != null)
        {
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
            fireEffectActive = false;
        }
    }

    /// <summary>
    /// Gets the current sprite renderer for external color manipulation.
    /// Used by other systems that need to flash or highlight the ball.
    /// </summary>
    public SpriteRenderer GetSpriteRenderer()
    {
        return spriteRenderer;
    }

    #endregion
}
