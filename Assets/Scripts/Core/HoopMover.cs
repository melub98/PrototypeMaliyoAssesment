using UnityEngine;

/// <summary>
/// Moves the hoop horizontally to the left.
/// Destroys the hoop when it goes off-screen.
/// Attach to the parent hoop GameObject.
/// </summary>
public class HoopMover : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Override speed (0 = use GameManager.gameSpeed)")]
    [SerializeField] private float overrideSpeed = 0f;

    [Tooltip("X position at which hoop is destroyed")]
    [SerializeField] private float destroyXPosition = -12f;

    private float moveSpeed;
    private bool isMoving = true;

    void Start()
    {
        // Set move speed
        if (overrideSpeed > 0)
        {
            moveSpeed = overrideSpeed;
        }
        else if (GameManager.Instance != null)
        {
            moveSpeed = GameManager.Instance.gameSpeed;
        }
        else
        {
            moveSpeed = 3f;
        }

        // Subscribe to game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }
    }

    void Update()
    {
        if (!isMoving) return;

        // Check if game is playing
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // Move left
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

        // Destroy when off-screen
        if (transform.position.x < destroyXPosition)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }
    }

    void OnGameOver()
    {
        isMoving = false;
    }

    /// <summary>
    /// Sets the movement speed.
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
}
