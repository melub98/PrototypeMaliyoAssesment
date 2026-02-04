using UnityEngine;

/// <summary>
/// Controls horizontal platform movement from right to left.
/// Platforms scroll across the screen and are destroyed when off-screen.
/// Movement stops on game over.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class PlatformObstacle : MonoBehaviour
{
    #region Serialized Fields

    [Header("Settings")]
    [Tooltip("X position at which platform is destroyed (off-screen left)")]
    [SerializeField] private float destroyXPosition = -12f;

    [Tooltip("Override movement speed (0 = use GameManager speed)")]
    [SerializeField] private float speedOverride = 0f;

    #endregion

    #region Private Fields

    // Movement speed (set from GameManager)
    private float moveSpeed;
    // Tracks if movement should stop
    private bool isStopped = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        // Get speed from GameManager or use override
        if (speedOverride > 0)
        {
            moveSpeed = speedOverride;
        }
        else if (GameManager.Instance != null)
        {
            moveSpeed = GameManager.Instance.gameSpeed;
        }
        else
        {
            moveSpeed = 3f; // Default fallback
        }

        // Subscribe to stop on game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(StopMoving);
        }
    }

    void Update()
    {
        // Don't move if stopped
        if (isStopped) return;

        // Check if game is playing (handles null safely)
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        // Move platform to the left
        // Uses Time.deltaTime so slow motion affects platform speed
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // Destroy when off-screen
        if (transform.position.x < destroyXPosition)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent errors if GameManager is destroyed first
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(StopMoving);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called on game over - stops platform movement.
    /// </summary>
    void StopMoving()
    {
        isStopped = true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the movement speed.
    /// </summary>
    /// <param name="speed">New movement speed</param>
    public void SetSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0, speed);
    }

    /// <summary>
    /// Gets the current movement speed.
    /// </summary>
    public float GetSpeed() => moveSpeed;

    #endregion
}
