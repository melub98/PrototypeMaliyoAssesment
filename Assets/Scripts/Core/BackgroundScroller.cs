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
    private BallController playerBall;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
            GameManager.Instance.OnRevive.AddListener(OnRevive);
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
        if (background1 == null || background2 == null) return;

        // Pause scrolling when ball is on a hoop rim
        if (playerBall == null)
            playerBall = Object.FindFirstObjectByType<BallController>();
        if (playerBall != null && playerBall.IsOnRim) return;

        float speed = GetScrollSpeed();
        float movement = speed * Time.deltaTime;

        // Move both backgrounds left
        background1.position += Vector3.left * movement;
        background2.position += Vector3.left * movement;

        // When a background scrolls fully off-screen, wrap it by exactly
        // 2x the width so there's never a gap between the two images.
        float doubleWidth = backgroundWidth * 2f;

        if (background1.position.x <= -backgroundWidth)
        {
            background1.position += Vector3.right * doubleWidth;
        }

        if (background2.position.x <= -backgroundWidth)
        {
            background2.position += Vector3.right * doubleWidth;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
            GameManager.Instance.OnRevive.RemoveListener(OnRevive);
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

    void OnRevive()
    {
        isScrolling = true;
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
