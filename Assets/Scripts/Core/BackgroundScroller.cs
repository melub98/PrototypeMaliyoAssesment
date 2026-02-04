using UnityEngine;

/// <summary>
/// Scrolls a background image continuously to create parallax effect.
/// Uses two sprites that swap positions for seamless infinite scrolling.
/// Speed syncs with GameManager.gameSpeed.
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("Background Sprites")]
    [Tooltip("First background sprite")]
    [SerializeField] private Transform background1;

    [Tooltip("Second background sprite (duplicate of first)")]
    [SerializeField] private Transform background2;

    [Header("Settings")]
    [Tooltip("Width of each background sprite")]
    [SerializeField] private float backgroundWidth = 20f;

    [Tooltip("Speed multiplier relative to game speed (1 = same speed, 0.5 = half speed for parallax)")]
    [SerializeField] private float speedMultiplier = 0.5f;

    [Tooltip("Use custom speed instead of GameManager speed")]
    [SerializeField] private bool useCustomSpeed = false;

    [Tooltip("Custom scroll speed (only used if useCustomSpeed is true)")]
    [SerializeField] private float customSpeed = 2f;

    private bool isScrolling = false;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }

        // Auto-detect background width if sprites have SpriteRenderer
        if (background1 != null)
        {
            SpriteRenderer sr = background1.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                backgroundWidth = sr.bounds.size.x;
            }
        }

        // Position backgrounds side by side
        if (background1 != null && background2 != null)
        {
            background2.position = background1.position + Vector3.right * backgroundWidth;
        }
    }

    void Update()
    {
        if (!isScrolling) return;

        float speed = GetScrollSpeed();
        float movement = speed * Time.deltaTime;

        // Move both backgrounds left
        if (background1 != null)
        {
            background1.position += Vector3.left * movement;

            // Check if background1 is off-screen left
            if (background1.position.x <= -backgroundWidth)
            {
                // Move it to the right of background2
                background1.position = background2.position + Vector3.right * backgroundWidth;
            }
        }

        if (background2 != null)
        {
            background2.position += Vector3.left * movement;

            // Check if background2 is off-screen left
            if (background2.position.x <= -backgroundWidth)
            {
                // Move it to the right of background1
                background2.position = background1.position + Vector3.right * backgroundWidth;
            }
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }
    }

    float GetScrollSpeed()
    {
        if (useCustomSpeed)
        {
            return customSpeed;
        }

        if (GameManager.Instance != null)
        {
            return GameManager.Instance.gameSpeed * speedMultiplier;
        }

        return customSpeed;
    }

    void OnGameStart()
    {
        isScrolling = true;
    }

    void OnGameOver()
    {
        isScrolling = false;
    }

    /// <summary>
    /// Manually start scrolling (useful for menu backgrounds).
    /// </summary>
    public void StartScrolling()
    {
        isScrolling = true;
    }

    /// <summary>
    /// Manually stop scrolling.
    /// </summary>
    public void StopScrolling()
    {
        isScrolling = false;
    }

    /// <summary>
    /// Set custom scroll speed.
    /// </summary>
    public void SetSpeed(float speed)
    {
        customSpeed = speed;
        useCustomSpeed = true;
    }
}
