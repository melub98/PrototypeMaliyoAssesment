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
/// - Ball rotates freely via Unity physics (freezeRotation disabled at runtime)
/// - Uses extra gravity when falling for snappier, more responsive controls
///
/// ARCHITECTURE:
/// - Subscribes to GameManager events for game state changes
/// - Decoupled from other systems through event-driven communication
/// - Shield visual is a separate GameObject for easy customization
///
/// SPRITE SWAP FEATURE:
/// - Ball swaps to a different sprite on clean passes to match the multiplier
/// - Sprites progress through variants as multiplier increases (1x, 2x, 4x, 8x, 16x)
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

    [Header("Spin")]
    [Tooltip("Torque applied on each jump (negative = clockwise). Clamped by max angular velocity")]
    [SerializeField] private float jumpTorque = -150f;

    [Tooltip("Maximum angular velocity in degrees/sec. Caps spin so collisions can override it")]
    [SerializeField] private float maxAngularVelocity = 360f;

    [Header("Rim Push")]
    [Tooltip("Horizontal push force when jumping while touching hoop rim")]
    [SerializeField] private float rimPushForce = 1.5f;

    [Tooltip("Max horizontal drift allowed when on hoop rim before snapping back")]
    [SerializeField] private float maxRimDrift = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip shieldBlockSound;

    [Header("Visual")]
    [Tooltip("GameObject shown around ball when shield is active")]
    [SerializeField] private GameObject shieldVisual;

    [Header("Multiplier Ball Sprites")]
    [Tooltip("Default ball sprite (1x / no multiplier)")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("Ball sprite at 2x multiplier")]
    [SerializeField] private Sprite sprite2x;

    [Tooltip("Ball sprite at 3x+ multiplier")]
    [SerializeField] private Sprite sprite3xPlus;

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

    // Fire effect tracking
    private ParticleSystem.EmissionModule fireEmission;
    private bool fireEffectActive = false;
    private int currentMultiplier = 1;

    // Cached UIManager reference for shield indicator
    private UIManager uiManager;

    // Hoop edge contact tracking - allows rim push when jumping on rim
    private int hoopEdgeContactCount = 0;

    // Invincibility after shield breaks
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private const float INVINCIBILITY_DURATION = 2f;
    private const float FLASH_INTERVAL = 0.1f;
    private float flashTimer = 0f;

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

    /// <summary>
    /// Whether the ball is in post-shield invincibility frames.
    /// </summary>
    public bool IsInvincible => isInvincible;

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

        // Ensure rotation is NOT frozen so Unity physics handles spinning naturally
        rb.freezeRotation = false;

        // Store start position for reset functionality
        startPosition = transform.position;
        lockedXPosition = startPosition.x;

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

        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
            GameManager.Instance.OnMultiplierChanged.AddListener(OnMultiplierChanged);
            GameManager.Instance.OnCleanPass.AddListener(OnCleanPass);
        }

        UpdateShieldVisual();

        // Apply initial sprite
        if (spriteRenderer != null && defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
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
            if (hoopEdgeContactCount <= 0)
            {
                // Normal: hard lock to X position
                LockHorizontalPosition();
            }
            else
            {
                // On rim: soft leash - allow small drift but don't get left behind
                SoftLockHorizontalPosition();
            }
        }

        // Invincibility countdown and sprite flash
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                // Ensure sprite is fully visible when invincibility ends
                if (spriteRenderer != null)
                    spriteRenderer.enabled = true;
            }
            else
            {
                // Flash the sprite on and off
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    flashTimer = FLASH_INTERVAL;
                    if (spriteRenderer != null)
                        spriteRenderer.enabled = !spriteRenderer.enabled;
                }
            }
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

        // Clamp angular velocity so ball doesn't spin too fast
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -maxAngularVelocity, maxAngularVelocity);
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
    /// Rotation is handled entirely by Unity physics (friction on collisions).
    /// When on a hoop rim, also pushes horizontally so the ball
    /// can roll up against the edge into the hoop or fall off.
    /// </summary>
    void Jump()
    {
        if (hoopEdgeContactCount > 0)
        {
            // On hoop rim: jump + push toward rim
            rb.linearVelocity = new Vector2(rb.linearVelocity.x + rimPushForce, jumpForce);
        }
        else
        {
            // In air: normal jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Add spin on jump - clamped in FixedUpdate so it can't overpower collision physics
        rb.AddTorque(jumpTorque);
        PlaySound(jumpSound);
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

    /// <summary>
    /// Soft leash when on hoop rim - allows small horizontal drift
    /// so the ball can push against the edge, but clamps it so
    /// the ball doesn't get left behind by the scrolling screen.
    /// </summary>
    void SoftLockHorizontalPosition()
    {
        Vector3 pos = transform.position;
        float drift = pos.x - lockedXPosition;

        if (Mathf.Abs(drift) > maxRimDrift)
        {
            pos.x = lockedXPosition + Mathf.Sign(drift) * maxRimDrift;
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
        isInvincible = false;
        hasHitFloorAfterDeath = false;
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        UpdateShieldVisual();

        // Reset position and rotation
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        hoopEdgeContactCount = 0;

        // Reset sprite to default
        currentMultiplier = 1;
        if (spriteRenderer != null && defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
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

        // Enable gravity so ball falls to ground
        rb.gravityScale = 1.5f; // Slightly higher for dramatic fall

        // Add random spin on death for tumble effect
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

        // Swap ball sprite based on multiplier
        if (spriteRenderer != null)
        {
            Sprite newSprite = GetSpriteForMultiplier(multiplier);
            if (newSprite != null)
            {
                spriteRenderer.sprite = newSprite;
            }
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

            // Update fire color based on multiplier
            var main = fireEffect.main;
            main.startColor = GetFireColorForMultiplier(multiplier);
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
    /// Returns the appropriate sprite for a given multiplier value.
    /// </summary>
    Sprite GetSpriteForMultiplier(int multiplier)
    {
        switch (multiplier)
        {
            case 1: return defaultSprite;
            case 2: return sprite2x;
            default: return sprite3xPlus; // 3x and above
        }
    }

    /// <summary>
    /// Returns fire particle color based on multiplier.
    /// </summary>
    Color GetFireColorForMultiplier(int multiplier)
    {
        if (multiplier <= 4) return new Color(1f, 0.6f, 0f, 0.8f);      // Orange fire
        if (multiplier <= 8) return new Color(1f, 0.3f, 0f, 0.9f);      // Red-orange fire
        return new Color(1f, 0.1f, 0.5f, 1f);                           // Purple fire (9x+)
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
        // Track hoop edge contacts for rim push physics
        if (collision.gameObject.GetComponent<HoopEdgeCollider>() != null)
        {
            hoopEdgeContactCount++;
        }

        // After death, detect floor hit to show game over UI
        if (isDead)
        {
            if (collision.gameObject.CompareTag("Boundary") && !hasHitFloorAfterDeath)
            {
                hasHitFloorAfterDeath = true;
                GameManager.Instance?.ShowGameOverUI();
                Debug.Log("BallController: Hit floor after death - showing game over UI");
            }
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (collision.gameObject.CompareTag("Boundary"))
        {
            HandleBoundaryHit();
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // Track leaving hoop edge
        if (collision.gameObject.GetComponent<HoopEdgeCollider>() != null)
        {
            hoopEdgeContactCount = Mathf.Max(0, hoopEdgeContactCount - 1);
        }
    }

    /// <summary>
    /// Catches the case where the ball is already on the floor when death occurs.
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
            StartInvincibility();

            // Bounce away from boundary
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.5f);
            Debug.Log("BallController: Shield absorbed boundary hit");
            return;
        }

        if (isInvincible)
        {
            // Still in invincibility frames - bounce but don't die
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.5f);
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
    /// Starts 2-second invincibility with sprite flashing.
    /// </summary>
    public bool UseShield()
    {
        if (isInvincible) return true;

        if (hasShield)
        {
            hasShield = false;
            UpdateShieldVisual();
            PlaySound(shieldBlockSound);
            StartInvincibility();
            return true;
        }
        return false;
    }

    void StartInvincibility()
    {
        isInvincible = true;
        invincibilityTimer = INVINCIBILITY_DURATION;
        flashTimer = FLASH_INTERVAL;
    }

    /// <summary>
    /// Updates the shield visual to match current shield state.
    /// Shield visual is a separate GameObject for easy customization.
    /// </summary>
    void UpdateShieldVisual()
    {
        // Only toggle shieldVisual if it's a scene object (not a prefab asset).
        // Calling SetActive on a prefab asset modifies the asset itself, breaking future spawns.
        if (shieldVisual != null && shieldVisual.scene.IsValid())
        {
            shieldVisual.SetActive(hasShield);
        }

        // Update the UI shield indicator
        if (uiManager == null)
            uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager != null)
            uiManager.SetShieldIndicator(hasShield);
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
        hoopEdgeContactCount = 0;
        isDead = false;
        hasShield = false;
        isInvincible = false;
        hasHitFloorAfterDeath = false;
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        UpdateShieldVisual();

        // Reset sprite to default
        currentMultiplier = 1;
        if (spriteRenderer != null && defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
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
