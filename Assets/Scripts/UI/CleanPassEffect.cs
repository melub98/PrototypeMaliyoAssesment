using UnityEngine;
using TMPro;

/// <summary>
/// Visual and audio feedback for clean passes through hoops.
/// Shows "CLEAN!" or "PERFECT!" text animation when triggered.
/// </summary>
public class CleanPassEffect : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Text that displays 'CLEAN!' or 'PERFECT!'")]
    [SerializeField] private TextMeshProUGUI cleanText;

    [Header("Text Options")]
    [SerializeField] private string[] cleanPassTexts = { "CLEAN!", "PERFECT!", "NICE!" };

    [Header("Animation")]
    [Tooltip("How long the text is visible")]
    [SerializeField] private float displayDuration = 0.8f;

    [Tooltip("Starting scale")]
    [SerializeField] private float startScale = 0.5f;

    [Tooltip("Peak scale")]
    [SerializeField] private float peakScale = 1.2f;

    [Tooltip("Time to reach peak scale")]
    [SerializeField] private float scaleUpTime = 0.15f;

    [Header("Colors")]
    [SerializeField] private Color textColor = Color.yellow;

    [Header("Audio")]
    [SerializeField] private AudioClip cleanPassSound;

    private AudioSource audioSource;
    private float animTimer = 0f;
    private bool isAnimating = false;
    private Vector3 baseScale;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (cleanText != null)
        {
            baseScale = cleanText.transform.localScale;
            cleanText.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCleanPass.AddListener(OnCleanPass);
        }
    }

    void Update()
    {
        if (!isAnimating || cleanText == null) return;

        animTimer += Time.deltaTime;

        if (animTimer >= displayDuration)
        {
            // End animation
            isAnimating = false;
            cleanText.gameObject.SetActive(false);
            cleanText.transform.localScale = baseScale;
        }
        else if (animTimer < scaleUpTime)
        {
            // Scale up phase
            float t = animTimer / scaleUpTime;
            float scale = Mathf.Lerp(startScale, peakScale, t);
            cleanText.transform.localScale = baseScale * scale;

            // Fade in
            Color c = cleanText.color;
            c.a = t;
            cleanText.color = c;
        }
        else
        {
            // Hold and fade out phase
            float t = (animTimer - scaleUpTime) / (displayDuration - scaleUpTime);
            float scale = Mathf.Lerp(peakScale, 1f, t * 0.5f);
            cleanText.transform.localScale = baseScale * scale;

            // Fade out in last half
            if (t > 0.5f)
            {
                Color c = cleanText.color;
                c.a = 1f - ((t - 0.5f) * 2f);
                cleanText.color = c;
            }
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCleanPass.RemoveListener(OnCleanPass);
        }
    }

    void OnCleanPass()
    {
        TriggerEffect();
    }

    /// <summary>
    /// Triggers the clean pass visual and audio effect.
    /// </summary>
    public void TriggerEffect()
    {
        if (cleanText != null)
        {
            // Pick random text
            if (cleanPassTexts != null && cleanPassTexts.Length > 0)
            {
                cleanText.text = cleanPassTexts[Random.Range(0, cleanPassTexts.Length)];
            }
            else
            {
                cleanText.text = "CLEAN!";
            }

            // Setup initial state
            cleanText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            cleanText.transform.localScale = baseScale * startScale;
            cleanText.gameObject.SetActive(true);

            // Start animation
            isAnimating = true;
            animTimer = 0f;
        }

        // Play sound
        if (cleanPassSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cleanPassSound);
        }
    }
}
