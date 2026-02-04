using UnityEngine;

/// <summary>
/// Main controller for a basketball hoop obstacle.
/// Tracks player pass state and coordinates between scoring zone and edge colliders.
/// The hoop has a U-shaped rim that the ball can roll on.
/// </summary>
public class HoopController : MonoBehaviour
{
    [Header("State Tracking")]
    public bool playerEnteredZone = false;
    public bool playerTouchedEdges = false;
    public bool hoopCleared = false;

    [Header("Settings")]
    [Tooltip("Base points awarded for passing through")]
    [SerializeField] private int basePoints = 1;

    [Header("Movement (Optional)")]
    [Tooltip("Enable vertical oscillation")]
    [SerializeField] private bool enableMovement = false;

    [Tooltip("Vertical movement range")]
    [SerializeField] private float moveRange = 1.5f;

    [Tooltip("Movement speed")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip scoreSound;
    [SerializeField] private AudioClip cleanPassSound;
    [SerializeField] private AudioClip rimBounceSound;

    private AudioSource audioSource;
    private float startY;
    private float moveTime;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        startY = transform.position.y;
        moveTime = Random.Range(0f, Mathf.PI * 2f); // Random start phase
    }

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

            // Play rim bounce sound
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

            // Award score
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

    /// <summary>
    /// Enables/disables vertical movement.
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
    }
}
