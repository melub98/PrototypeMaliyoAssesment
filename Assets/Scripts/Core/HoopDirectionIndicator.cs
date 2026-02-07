using UnityEngine;

/// <summary>
/// Displays animated directional arrows inside a hoop to indicate
/// which direction the player should enter.
/// Uses the same bob + rotation animation style as ShieldPowerUp.
/// Attach to a child GameObject of the hoop that has a SpriteRenderer with an arrow sprite.
/// </summary>
public class HoopDirectionIndicator : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Bob animation amplitude")]
    [SerializeField] private float bobAmplitude = 0.15f;

    [Tooltip("Bob animation speed")]
    [SerializeField] private float bobSpeed = 2f;

    [Tooltip("Pulse scale amount (0 = no pulse)")]
    [SerializeField] private float pulseAmount = 0.1f;

    [Tooltip("Pulse speed")]
    [SerializeField] private float pulseSpeed = 3f;

    [Header("Visual")]
    [Tooltip("Arrow color (blue by default)")]
    [SerializeField] private Color arrowColor = new Color(0.2f, 0.6f, 1f, 0.9f);

    private float animTime = 0f;
    private Vector3 startLocalPos;
    private Vector3 baseScale;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        startLocalPos = transform.localPosition;
        baseScale = transform.localScale;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = arrowColor;
        }
    }

    void Update()
    {
        animTime += Time.deltaTime;

        // Bob up and down (local space so it moves with the hoop)
        Vector3 pos = startLocalPos;
        pos.y += Mathf.Sin(animTime * bobSpeed) * bobAmplitude;
        transform.localPosition = pos;

        // Gentle pulse scale
        if (pulseAmount > 0)
        {
            float scale = 1f + Mathf.Sin(animTime * pulseSpeed) * pulseAmount;
            transform.localScale = baseScale * scale;
        }
    }
}
