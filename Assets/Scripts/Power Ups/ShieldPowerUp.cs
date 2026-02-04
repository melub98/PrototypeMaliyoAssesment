using UnityEngine;

/// <summary>
/// Shield power-up collectible.
/// Grants player a shield that absorbs one boundary hit.
/// Moves left with the hoops and can be collected by touching it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShieldPowerUp : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Override speed (0 = use GameManager.gameSpeed)")]
    [SerializeField] private float overrideSpeed = 0f;

    [Tooltip("X position at which power-up is destroyed")]
    [SerializeField] private float destroyXPosition = -12f;

    [Header("Animation")]
    [Tooltip("Rotation speed in degrees per second")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Bob animation amplitude")]
    [SerializeField] private float bobAmplitude = 0.2f;

    [Tooltip("Bob animation speed")]
    [SerializeField] private float bobSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;

    [Header("Visual")]
    [SerializeField] private Color shieldColor = Color.cyan;

    private float moveSpeed;
    private float originalY;
    private float animTime = 0f;
    private bool isCollected = false;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Get sprite renderer and apply color
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = shieldColor;
        }

        originalY = transform.position.y;
    }

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
        if (isCollected) return;

        // Check if game is playing
        bool isPlaying = GameManager.Instance != null && GameManager.Instance.IsPlaying;

        if (isPlaying)
        {
            // Move left
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

            // Animation
            animTime += Time.deltaTime;

            // Bob up and down
            if (bobAmplitude > 0)
            {
                float newY = originalY + Mathf.Sin(animTime * bobSpeed) * bobAmplitude;
                Vector3 pos = transform.position;
                pos.y = newY;
                transform.position = pos;
            }

            // Rotate
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

            // Destroy when off-screen
            if (transform.position.x < destroyXPosition)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
        }
    }

    void Collect(GameObject player)
    {
        isCollected = true;

        // Grant shield to player
        BallController ball = player.GetComponent<BallController>();
        if (ball != null)
        {
            ball.GrantShield();
        }

        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        Debug.Log("ShieldPowerUp: Collected!");

        Destroy(gameObject);
    }

    void OnGameOver()
    {
        moveSpeed = 0;
    }
}
