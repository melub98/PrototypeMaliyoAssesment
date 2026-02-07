using UnityEngine;
using TMPro;

/// <summary>
/// Displays an FPS counter for performance debugging.
/// Attach to a GameObject with a TextMeshProUGUI component, or leave
/// textDisplay null to render via OnGUI overlay (no UI setup needed).
/// </summary>
public class FPSCounter : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("Optional TMP text to display FPS. If null, uses OnGUI overlay.")]
    [SerializeField] private TextMeshProUGUI textDisplay;

    [Tooltip("How often the display updates (seconds)")]
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Threshold Colors")]
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color warnColor = Color.yellow;
    [SerializeField] private Color badColor = Color.red;

    [Tooltip("FPS below this is bad (red)")]
    [SerializeField] private int badThreshold = 25;
    [Tooltip("FPS below this is warning (yellow)")]
    [SerializeField] private int warnThreshold = 35;

    private float timer = 0f;
    private int frameCount = 0;
    private float currentFPS = 0f;
    private string fpsText = "";
    private Color currentColor;

    // OnGUI style (used when no TMP text assigned)
    private GUIStyle guiStyle;

    void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            currentFPS = frameCount / timer;
            frameCount = 0;
            timer = 0f;

            int fps = Mathf.RoundToInt(currentFPS);
            fpsText = $"FPS: {fps}";

            if (fps < badThreshold)
                currentColor = badColor;
            else if (fps < warnThreshold)
                currentColor = warnColor;
            else
                currentColor = goodColor;

            if (textDisplay != null)
            {
                textDisplay.text = fpsText;
                textDisplay.color = currentColor;
            }
        }
    }

    void OnGUI()
    {
        // Only use OnGUI if no TMP text is assigned
        if (textDisplay != null) return;

        if (guiStyle == null)
        {
            guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.fontSize = 24;
            guiStyle.fontStyle = FontStyle.Bold;
        }

        guiStyle.normal.textColor = currentColor;
        GUI.Label(new Rect(10, 10, 200, 40), fpsText, guiStyle);
    }
}
