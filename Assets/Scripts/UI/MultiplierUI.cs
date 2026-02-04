using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current score multiplier with color coding and animations.
/// Colors: 1x=white, 2x=yellow, 4x=orange, 8x=red, 16x=purple
/// </summary>
public class MultiplierUI : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Text displaying the multiplier value")]
    [SerializeField] private TextMeshProUGUI multiplierText;

    [Header("Colors")]
    [SerializeField] private Color color1x = Color.white;
    [SerializeField] private Color color2x = Color.yellow;
    [SerializeField] private Color color4x = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color color8x = Color.red;
    [SerializeField] private Color color16x = new Color(0.5f, 0f, 1f); // Purple

    [Header("Animation")]
    [Tooltip("Scale multiplier when multiplier increases")]
    [SerializeField] private float pulseScale = 1.3f;

    [Tooltip("Duration of pulse animation")]
    [SerializeField] private float pulseDuration = 0.2f;

    [Header("Visibility")]
    [Tooltip("Hide when multiplier is 1x")]
    [SerializeField] private bool hideAt1x = true;

    private Vector3 originalScale;
    private float pulseTimer = 0f;
    private bool isPulsing = false;

    void Awake()
    {
        if (multiplierText != null)
        {
            originalScale = multiplierText.transform.localScale;
        }
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMultiplierChanged.AddListener(OnMultiplierChanged);
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
        }

        // Initial state
        UpdateDisplay(1);
    }

    void Update()
    {
        // Handle pulse animation
        if (isPulsing && multiplierText != null)
        {
            pulseTimer += Time.deltaTime;
            float t = pulseTimer / pulseDuration;

            if (t >= 1f)
            {
                isPulsing = false;
                multiplierText.transform.localScale = originalScale;
            }
            else
            {
                // Scale up then back down
                float scale = 1f + (pulseScale - 1f) * (1f - Mathf.Abs(2f * t - 1f));
                multiplierText.transform.localScale = originalScale * scale;
            }
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMultiplierChanged.RemoveListener(OnMultiplierChanged);
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
        }
    }

    void OnGameStart()
    {
        UpdateDisplay(1);
    }

    void OnMultiplierChanged(int multiplier)
    {
        UpdateDisplay(multiplier);

        // Trigger pulse animation if multiplier increased
        if (multiplier > 1)
        {
            TriggerPulse();
        }
    }

    void UpdateDisplay(int multiplier)
    {
        if (multiplierText == null) return;

        // Set text
        multiplierText.text = $"x{multiplier}";

        // Set color based on multiplier
        multiplierText.color = GetMultiplierColor(multiplier);

        // Show/hide based on settings
        if (hideAt1x)
        {
            multiplierText.gameObject.SetActive(multiplier > 1);
        }
    }

    Color GetMultiplierColor(int multiplier)
    {
        switch (multiplier)
        {
            case 1: return color1x;
            case 2: return color2x;
            case 4: return color4x;
            case 8: return color8x;
            default: return color16x; // 16x and above
        }
    }

    void TriggerPulse()
    {
        isPulsing = true;
        pulseTimer = 0f;
    }

    /// <summary>
    /// Manually refresh the display.
    /// </summary>
    public void RefreshDisplay()
    {
        if (GameManager.Instance != null)
        {
            UpdateDisplay(GameManager.Instance.CurrentMultiplier);
        }
    }
}
