using UnityEngine;

/// <summary>
/// Makes a hoop cycle between visible and ghost (low opacity) states.
/// Purely visual effect - all colliders stay active at all times.
/// Player must still score through it or die on miss, same as normal hoops.
///
/// Attach to the same GameObject as HoopController.
/// The hoop prefab should use a grey color for its sprites.
/// </summary>
public class GhostHoopEffect : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("How long the hoop stays fully visible")]
    [SerializeField] private float visibleDuration = 1.0f;

    [Tooltip("How long the hoop stays in ghost (faded) state")]
    [SerializeField] private float ghostDuration = 0.7f;

    [Tooltip("How fast the fade transition is")]
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Ghost Appearance")]
    [Tooltip("Alpha when in ghost state (0 = invisible, 1 = fully visible)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float ghostAlpha = 0.15f;

    private SpriteRenderer[] spriteRenderers;
    private HoopController hoopController;

    private float cycleTimer = 0f;
    private bool isGhost = false;
    private float currentAlpha = 1f;
    private float targetAlpha = 1f;

    void Awake()
    {
        hoopController = GetComponent<HoopController>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // Don't cycle if hoop is already cleared
        if (hoopController != null && hoopController.hoopCleared) return;

        cycleTimer += Time.deltaTime;

        float currentPhaseDuration = isGhost ? ghostDuration : visibleDuration;

        if (cycleTimer >= currentPhaseDuration)
        {
            cycleTimer = 0f;
            isGhost = !isGhost;
            targetAlpha = isGhost ? ghostAlpha : 1f;
        }

        // Smooth fade between states
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        ApplyAlpha(currentAlpha);
    }

    void ApplyAlpha(float alpha)
    {
        if (spriteRenderers == null) return;

        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }

    /// <summary>
    /// Whether the hoop is currently in ghost (faded) state.
    /// </summary>
    public bool IsGhost => isGhost;
}
