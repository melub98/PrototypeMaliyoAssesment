using UnityEngine;
using System.Collections;

/// <summary>
/// Main controller for a basketball hoop obstacle.
///
/// DESIGN DECISIONS:
/// - Acts as the central coordinator for all hoop-related components
/// - Child components (ScoringZone, EdgeCollider, MissDetector) report to this controller
/// - Tracks player's pass state: entered zone, touched edges, cleared hoop
/// - Determines whether pass was "clean" (no edge contact) for multiplier bonus
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

    [Header("Hoop Color")]
    [Tooltip("Color applied to all hoop sprites on spawn")]
    [SerializeField] private Color hoopColor = Color.white;

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

    #region Fire Effect

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
    private bool isGhostHoop;

    // Movement tracking
    private float startY;
    private float moveTime;

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

        // Cache sprite renderers and apply hoop color
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
        {
            if (sr != null) sr.color = hoopColor;
        }

        // Store initial Y position for oscillation
        startY = transform.position.y;
        moveTime = Random.Range(0f, Mathf.PI * 2f);

        // Cache ghost hoop check (avoids GetComponent at score time)
        isGhostHoop = GetComponent<GhostHoopEffect>() != null;

        // Setup fire effect if assigned
        if (fireEffect != null)
        {
            fireEmission = fireEffect.emission;
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Unity Update - Handle movement.
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
            // Only play sound and log on initial contact, not re-entries while rolling
            if (!playerTouchedEdges)
            {
                playerTouchedEdges = true;

                if (rimBounceSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(rimBounceSound, 0.5f);
                }

                Debug.Log("HoopController: Player touched hoop rim");
            }
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
                // Ghost hoop clean pass gives instant x8 multiplier
                if (wasCleanPass && isGhostHoop)
                {
                    GameManager.Instance.SetMultiplier(8);
                }

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

            // Fade out and destroy hoop after player clears it
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    IEnumerator FadeOutAndDestroy()
    {
        // Disable colliders so the cleared hoop doesn't interact with anything
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // Wait before fading
        yield return new WaitForSeconds(0.2f);

        float fadeDuration = 0.2f;
        float elapsed = 0f;

        // Capture starting alpha for each renderer
        float[] startAlphas = new float[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            startAlphas[i] = spriteRenderers[i] != null ? spriteRenderers[i].color.a : 1f;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color c = spriteRenderers[i].color;
                    c.a = Mathf.Lerp(startAlphas[i], 0f, t);
                    spriteRenderers[i].color = c;
                }
            }

            yield return null;
        }

        Destroy(gameObject);
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

    #region Fire Effect Methods

    /// <summary>
    /// Sets the multiplier level for this hoop's fire effect.
    /// Called by HoopSpawner when creating the hoop.
    /// Fire activates at threshold and intensity scales with multiplier.
    /// </summary>
    public void SetMultiplierVisuals(int multiplier)
    {
        currentMultiplier = multiplier;
        UpdateFireEffect(multiplier);
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
            fireEffect.gameObject.SetActive(true);
            fireEffect.Play();
            fireEffectActive = true;
        }
        else if (!shouldShowFire && fireEffectActive)
        {
            fireEffect.Stop();
            fireEffect.gameObject.SetActive(false);
            fireEffectActive = false;
        }

        // Scale fire intensity with multiplier
        if (shouldShowFire && scaleFireWithMultiplier)
        {
            float intensity = Mathf.Lerp(baseFireEmission, maxFireEmission,
                (multiplier - fireEffectThreshold) / 12f);
            fireEmission.rateOverTime = intensity;

            var main = fireEffect.main;
            main.startColor = GetFireColorForMultiplier(multiplier);
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

    /// <summary>
    /// Sets the base points this hoop awards. Used for special hoop types.
    /// </summary>
    public void SetBasePoints(int points)
    {
        basePoints = points;
    }

    /// <summary>
    /// Gets cached sprite renderers for external effects (e.g. ghost hoop).
    /// </summary>
    public SpriteRenderer[] GetSpriteRenderers() => spriteRenderers;

    #endregion
}
