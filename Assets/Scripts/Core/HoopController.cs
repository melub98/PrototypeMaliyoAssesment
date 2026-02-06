using UnityEngine;

/// <summary>
/// Main controller for a basketball hoop obstacle.
///
/// DESIGN DECISIONS:
/// - Acts as the central coordinator for all hoop-related components
/// - Child components (ScoringZone, EdgeCollider, MissDetector) report to this controller
/// - Tracks player's pass state: entered zone, touched edges, cleared hoop
/// - Determines whether pass was "clean" (no edge contact) for multiplier bonus
///
/// MULTIPLIER COLOR SYSTEM:
/// - Hoops spawn with color matching current multiplier (shows player's streak)
/// - Colors: white (1x) -> yellow (2x) -> orange (4x) -> red (8x) -> purple (16x)
/// - Fire effect activates at 4x multiplier and above for visual impact
/// - This gives players immediate feedback about their current streak
///
/// FIRE EFFECT:
/// - Particle system child object that shows flames around the hoop
/// - Activates when multiplier reaches threshold (default 4x)
/// - Intensity increases with higher multipliers
/// - Creates excitement and rewards skilled play
/// </summary>
public class HoopController : MonoBehaviour
{
    #region State Tracking

    [Header("State Tracking")]
    [Tooltip("True when ball has entered the scoring zone (top of hoop)")]
    public bool playerEnteredZone = false;

    [Tooltip("True when ball touched any edge/rim while passing through")]
    public bool playerTouchedEdges = false;

    [Tooltip("True when ball has successfully passed through and scored")]
    public bool hoopCleared = false;

    #endregion

    #region Settings

    [Header("Settings")]
    [Tooltip("Base points awarded for passing through. Multiplied by current multiplier for clean passes")]
    [SerializeField] private int basePoints = 1;

    #endregion

    #region Movement Settings

    [Header("Movement (Optional)")]
    [Tooltip("Enable vertical oscillation for added difficulty. Set by HoopSpawner based on score")]
    [SerializeField] private bool enableMovement = false;

    [Tooltip("Vertical movement range in units. Hoop oscillates this far up and down")]
    [SerializeField] private float moveRange = 1.5f;

    [Tooltip("Movement speed. Higher = faster oscillation")]
    [SerializeField] private float moveSpeed = 2f;

    #endregion

    #region Audio

    [Header("Audio")]
    [Tooltip("Sound played when scoring (with edge touch)")]
    [SerializeField] private AudioClip scoreSound;

    [Tooltip("Sound played for clean pass (no edge touch)")]
    [SerializeField] private AudioClip cleanPassSound;

    [Tooltip("Sound played when ball bounces off rim")]
    [SerializeField] private AudioClip rimBounceSound;

    #endregion

    #region Multiplier Visual Effects

    [Header("Multiplier Color Effect")]
    [Tooltip("Enable color change based on multiplier")]
    [SerializeField] private bool enableColorEffect = true;

    [Tooltip("Default hoop color (1x multiplier)")]
    [SerializeField] private Color color1x = Color.white;

    [Tooltip("Color at 2x multiplier")]
    [SerializeField] private Color color2x = Color.yellow;

    [Tooltip("Color at 4x multiplier")]
    [SerializeField] private Color color4x = new Color(1f, 0.5f, 0f); // Orange

    [Tooltip("Color at 8x multiplier")]
    [SerializeField] private Color color8x = Color.red;

    [Tooltip("Color at 16x multiplier")]
    [SerializeField] private Color color16x = new Color(0.8f, 0f, 1f); // Purple

    [Tooltip("How fast colors transition")]
    [SerializeField] private float colorTransitionSpeed = 8f;

    [Header("Fire Effect")]
    [Tooltip("Particle system for fire effect (child object)")]
    [SerializeField] private ParticleSystem fireEffect;

    [Tooltip("Minimum multiplier to show fire effect")]
    [SerializeField] private int fireEffectThreshold = 4;

    [Tooltip("Scale fire intensity with multiplier")]
    [SerializeField] private bool scaleFireWithMultiplier = true;

    [Tooltip("Base emission rate for fire")]
    [SerializeField] private float baseFireEmission = 20f;

    [Tooltip("Max emission rate at max multiplier")]
    [SerializeField] private float maxFireEmission = 100f;

    #endregion

    #region Private Fields

    // Component references
    private AudioSource audioSource;
    private SpriteRenderer[] spriteRenderers;

    // Movement tracking
    private float startY;
    private float moveTime;

    // Color tracking
    private Color targetColor;
    private Color currentColor;
    private int currentMultiplier = 1;

    // Fire effect tracking
    private ParticleSystem.EmissionModule fireEmission;
    private bool fireEffectActive = false;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity Awake - Initialize components.
    /// </summary>
    void Awake()
    {
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Cache sprite renderers for color change
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        // Store initial Y position for oscillation
        startY = transform.position.y;
        moveTime = Random.Range(0f, Mathf.PI * 2f);

        // Initialize colors
        currentColor = color1x;
        targetColor = color1x;

        // Setup fire effect if assigned
        if (fireEffect != null)
        {
            fireEmission = fireEffect.emission;
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // Apply initial color immediately
        ApplyColorToRenderers(currentColor);
    }

    /// <summary>
    /// Unity Update - Handle movement and visual effects.
    /// </summary>
    void Update()
    {
        // Handle vertical oscillation if enabled
        if (enableMovement && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            moveTime += Time.deltaTime * moveSpeed;
            float newY = startY + Mathf.Sin(moveTime) * moveRange;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Smooth color transition
        if (enableColorEffect && spriteRenderers != null && spriteRenderers.Length > 0)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);
            ApplyColorToRenderers(currentColor);
        }
    }

    #endregion

    #region Player Interaction Methods

    /// <summary>
    /// Called by HoopScoringZone when player enters the hoop opening.
    /// </summary>
    public void OnPlayerEnterZone()
    {
        playerEnteredZone = true;
        Debug.Log("HoopController: Player entered hoop zone");
    }

    /// <summary>
    /// Called by HoopEdgeCollider when player touches the rim.
    /// </summary>
    public void OnPlayerTouchEdge()
    {
        if (!hoopCleared)
        {
            playerTouchedEdges = true;

            if (rimBounceSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(rimBounceSound, 0.5f);
            }

            Debug.Log("HoopController: Player touched hoop rim");
        }
    }

    /// <summary>
    /// Called by HoopScoringZone when player exits the zone.
    /// Awards score if player successfully passed through.
    /// </summary>
    public void OnPlayerExitZone()
    {
        if (hoopCleared) return;

        if (playerEnteredZone)
        {
            hoopCleared = true;
            bool wasCleanPass = !playerTouchedEdges;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(basePoints, wasCleanPass);
            }

            // Play appropriate sound
            if (wasCleanPass && cleanPassSound != null)
            {
                audioSource.PlayOneShot(cleanPassSound);
            }
            else if (scoreSound != null)
            {
                audioSource.PlayOneShot(scoreSound);
            }

            Debug.Log($"HoopController: Hoop cleared! Clean pass: {wasCleanPass}");
        }
    }

    /// <summary>
    /// Called by MissDetector when player passed the hoop without entering.
    /// </summary>
    public void OnPlayerMissedHoop()
    {
        if (hoopCleared) return;

        if (!playerEnteredZone)
        {
            Debug.Log("HoopController: Player missed the hoop!");
            GameManager.Instance?.GameOver();
        }
    }

    #endregion

    #region Multiplier Visual Methods

    /// <summary>
    /// Sets the multiplier level for this hoop's visual effects.
    /// Called by HoopSpawner when creating the hoop.
    ///
    /// VISUAL FEEDBACK:
    /// - Changes hoop color based on multiplier
    /// - Activates fire effect at threshold (4x+)
    /// - Fire intensity scales with multiplier
    /// </summary>
    public void SetMultiplierVisuals(int multiplier)
    {
        currentMultiplier = multiplier;

        // Set color based on multiplier
        targetColor = GetColorForMultiplier(multiplier);
        currentColor = targetColor; // Instant color on spawn
        ApplyColorToRenderers(currentColor);

        // Handle fire effect
        UpdateFireEffect(multiplier);

        Debug.Log($"HoopController: Set multiplier visuals to {multiplier}x");
    }

    /// <summary>
    /// Updates the fire effect based on current multiplier.
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
        if (shouldShowFire && scaleFireWithMultiplier)
        {
            float intensity = Mathf.Lerp(baseFireEmission, maxFireEmission,
                (multiplier - fireEffectThreshold) / 12f); // 4 to 16 range
            fireEmission.rateOverTime = intensity;

            // Also scale particle start color intensity
            var main = fireEffect.main;
            Color fireColor = GetFireColorForMultiplier(multiplier);
            main.startColor = fireColor;
        }
    }

    /// <summary>
    /// Returns the appropriate color for a given multiplier.
    /// </summary>
    Color GetColorForMultiplier(int multiplier)
    {
        switch (multiplier)
        {
            case 1: return color1x;
            case 2: return color2x;
            case 4: return color4x;
            case 8: return color8x;
            default: return color16x;
        }
    }

    /// <summary>
    /// Returns fire particle color based on multiplier.
    /// Higher multipliers = more intense fire colors.
    /// </summary>
    Color GetFireColorForMultiplier(int multiplier)
    {
        switch (multiplier)
        {
            case 4: return new Color(1f, 0.6f, 0f, 0.8f);      // Orange fire
            case 8: return new Color(1f, 0.3f, 0f, 0.9f);      // Red-orange fire
            default: return new Color(1f, 0.1f, 0.5f, 1f);     // Purple fire (16x+)
        }
    }

    /// <summary>
    /// Applies color to all sprite renderers.
    /// </summary>
    void ApplyColorToRenderers(Color color)
    {
        if (spriteRenderers == null) return;

        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Enables or disables vertical movement.
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        enableMovement = enabled;
    }

    /// <summary>
    /// Sets movement parameters.
    /// </summary>
    public void SetMovementParams(float range, float speed)
    {
        moveRange = range;
        moveSpeed = speed;
    }

    /// <summary>
    /// Resets the hoop state.
    /// </summary>
    public void ResetHoop()
    {
        playerEnteredZone = false;
        playerTouchedEdges = false;
        hoopCleared = false;

        targetColor = color1x;
        currentColor = color1x;
        ApplyColorToRenderers(currentColor);

        if (fireEffect != null)
        {
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
            fireEffectActive = false;
        }
    }

    /// <summary>
    /// Gets current multiplier level for this hoop.
    /// </summary>
    public int GetCurrentMultiplier() => currentMultiplier;

    #endregion
}
