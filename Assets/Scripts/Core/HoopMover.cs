using UnityEngine;

/// <summary>
/// Moves the hoop horizontally to the left.
/// Destroys the hoop when it goes off-screen.
/// Attach to the parent hoop GameObject.
/// Movement is controlled by GameManager.IsPlaying - stops on game over,
/// resumes on revive automatically.
/// </summary>
public class HoopMover : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Override speed (0 = use GameManager.gameSpeed)")]
    [SerializeField] private float overrideSpeed = 0f;

    [Tooltip("X position at which hoop is destroyed")]
    [SerializeField] private float destroyXPosition = -12f;

    private float moveSpeed;
    private BallController playerBall;

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
    }

    void Update()
    {
        // Only move when game is playing (auto-pauses on game over, auto-resumes on revive)
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // Pause scrolling when ball is on a hoop rim
        if (playerBall == null)
            playerBall = Object.FindFirstObjectByType<BallController>();
        if (playerBall != null && playerBall.IsOnRim) return;

        // Move left
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

        // Destroy when off-screen
        if (transform.position.x < destroyXPosition)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the movement speed.
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
}
