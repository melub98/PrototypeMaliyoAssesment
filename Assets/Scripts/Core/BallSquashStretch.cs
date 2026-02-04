using UnityEngine;

/// <summary>
/// Optional visual polish: applies squash and stretch deformation to the ball
/// based on its vertical velocity. Creates more dynamic, cartoon-like movement.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BallSquashStretch : MonoBehaviour
{
    [Header("Squash & Stretch Settings")]
    [Tooltip("Maximum amount of squash/stretch deformation")]
    [SerializeField] private float maxStretch = 0.3f;
    [Tooltip("How quickly the ball returns to normal scale")]
    [SerializeField] private float returnSpeed = 8f;
    [Tooltip("Velocity at which max stretch is reached")]
    [SerializeField] private float velocityReference = 10f;

    [Header("Impact Squash")]
    [Tooltip("Squash amount on impact")]
    [SerializeField] private float impactSquash = 0.4f;
    [Tooltip("Duration of impact squash effect")]
    [SerializeField] private float impactDuration = 0.1f;

    // Component references
    private Rigidbody2D rb;
    private Transform visualTransform;

    // State tracking
    private Vector3 baseScale;
    private float impactTimer = 0f;
    private bool isImpacting = false;

    /// <summary>
    /// Unity Awake - caches component references.
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Use this transform or find a child "Visual" transform
        visualTransform = transform.Find("Visual");
        if (visualTransform == null)
        {
            visualTransform = transform;
        }

        baseScale = visualTransform.localScale;
    }

    /// <summary>
    /// Unity LateUpdate - applies squash/stretch after physics.
    /// </summary>
    void LateUpdate()
    {
        if (!GameManager.Instance.IsPlaying)
        {
            // Reset to base scale when not playing
            visualTransform.localScale = baseScale;
            return;
        }

        Vector3 targetScale;

        // Check if we're in impact animation
        if (isImpacting)
        {
            impactTimer -= Time.deltaTime;
            if (impactTimer <= 0)
            {
                isImpacting = false;
            }

            // Squash on impact (wider, shorter)
            float t = 1f - (impactTimer / impactDuration);
            float squash = Mathf.Lerp(impactSquash, 0f, t);
            targetScale = new Vector3(
                baseScale.x * (1f + squash),
                baseScale.y * (1f - squash),
                baseScale.z
            );
        }
        else
        {
            // Calculate stretch based on velocity
            float velocityY = rb.linearVelocity.y;
            float stretchAmount = Mathf.Clamp(velocityY / velocityReference, -1f, 1f) * maxStretch;

            // Going up = stretch vertically (taller, thinner)
            // Going down = squash slightly (shorter, wider)
            if (velocityY > 0)
            {
                // Stretching up
                targetScale = new Vector3(
                    baseScale.x * (1f - stretchAmount * 0.5f),
                    baseScale.y * (1f + stretchAmount),
                    baseScale.z
                );
            }
            else
            {
                // Squashing down
                targetScale = new Vector3(
                    baseScale.x * (1f - stretchAmount * 0.5f),
                    baseScale.y * (1f + stretchAmount),
                    baseScale.z
                );
            }
        }

        // Smoothly interpolate to target scale
        visualTransform.localScale = Vector3.Lerp(
            visualTransform.localScale,
            targetScale,
            Time.deltaTime * returnSpeed
        );
    }

    /// <summary>
    /// Triggers an impact squash animation.
    /// Call this when the ball hits something or jumps.
    /// </summary>
    public void TriggerImpact()
    {
        isImpacting = true;
        impactTimer = impactDuration;
    }

    /// <summary>
    /// Unity collision callback - triggers impact squash on collision.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        TriggerImpact();
    }

    /// <summary>
    /// Resets the scale to base values.
    /// </summary>
    public void ResetScale()
    {
        visualTransform.localScale = baseScale;
        isImpacting = false;
    }
}
