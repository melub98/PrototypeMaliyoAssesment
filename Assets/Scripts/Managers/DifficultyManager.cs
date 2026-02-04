using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages game difficulty settings for Flappy Jump.
/// Controls: game speed, hoop spawn rate, ball size, and hoop rotations.
/// Ball is smaller on Easy (easier to fit through) and larger on Hard.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TextMeshProUGUI difficultyText;

    [Header("Easy Settings")]
    [SerializeField] private float easyGameSpeed = 2.5f;
    [SerializeField] private float easySpawnInterval = 2.5f;
    [SerializeField] private float easyBallScale = 0.8f;

    [Header("Medium Settings")]
    [SerializeField] private float mediumGameSpeed = 3.5f;
    [SerializeField] private float mediumSpawnInterval = 2f;
    [SerializeField] private float mediumBallScale = 1f;

    [Header("Hard Settings")]
    [SerializeField] private float hardGameSpeed = 5f;
    [SerializeField] private float hardSpawnInterval = 1.5f;
    [SerializeField] private float hardBallScale = 1.2f;

    [Header("Ball Reference")]
    [Tooltip("Reference to the player ball to scale")]
    [SerializeField] private Transform ballTransform;

    private int currentDifficulty = 1;
    private readonly string[] difficultyNames = { "Easy", "Medium", "Hard" };
    private Vector3 originalBallScale;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find ball if not assigned
        if (ballTransform == null)
        {
            GameObject ball = GameObject.FindGameObjectWithTag("Player");
            if (ball != null)
            {
                ballTransform = ball.transform;
            }
        }

        // Store original scale
        if (ballTransform != null)
        {
            originalBallScale = ballTransform.localScale;
        }

        // Setup slider
        if (difficultySlider != null)
        {
            difficultySlider.minValue = 0;
            difficultySlider.maxValue = 2;
            difficultySlider.wholeNumbers = true;
            difficultySlider.value = currentDifficulty;
            difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        }

        UpdateDifficultyDisplay();
        ApplyDifficulty();
    }

    void OnDifficultyChanged(float value)
    {
        currentDifficulty = Mathf.RoundToInt(value);
        UpdateDifficultyDisplay();
        ApplyDifficulty();
    }

    void UpdateDifficultyDisplay()
    {
        if (difficultyText != null)
        {
            difficultyText.text = difficultyNames[currentDifficulty];

            switch (currentDifficulty)
            {
                case 0:
                    difficultyText.color = Color.green;
                    break;
                case 1:
                    difficultyText.color = Color.yellow;
                    break;
                case 2:
                    difficultyText.color = Color.red;
                    break;
            }
        }
    }

    public void ApplyDifficulty()
    {
        float gameSpeed, spawnInterval, ballScale;

        switch (currentDifficulty)
        {
            case 0: // Easy
                gameSpeed = easyGameSpeed;
                spawnInterval = easySpawnInterval;
                ballScale = easyBallScale;
                break;
            case 2: // Hard
                gameSpeed = hardGameSpeed;
                spawnInterval = hardSpawnInterval;
                ballScale = hardBallScale;
                break;
            default: // Medium
                gameSpeed = mediumGameSpeed;
                spawnInterval = mediumSpawnInterval;
                ballScale = mediumBallScale;
                break;
        }

        // Apply to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.gameSpeed = gameSpeed;
        }

        // Apply to HoopSpawner
        if (HoopSpawner.Instance != null)
        {
            HoopSpawner.Instance.SetSpawnInterval(spawnInterval);
            HoopSpawner.Instance.SetDifficulty(currentDifficulty);
        }

        // Apply ball scale
        ApplyBallScale(ballScale);

        Debug.Log($"Difficulty: {difficultyNames[currentDifficulty]} - Speed={gameSpeed}, Interval={spawnInterval}, BallScale={ballScale}");
    }

    void ApplyBallScale(float scaleMultiplier)
    {
        if (ballTransform == null)
        {
            // Try to find ball again
            GameObject ball = GameObject.FindGameObjectWithTag("Player");
            if (ball != null)
            {
                ballTransform = ball.transform;
                originalBallScale = Vector3.one; // Assume default scale
            }
        }

        if (ballTransform != null)
        {
            ballTransform.localScale = originalBallScale * scaleMultiplier;

            // Also update the collider radius if it's a CircleCollider2D
            CircleCollider2D circleCol = ballTransform.GetComponent<CircleCollider2D>();
            if (circleCol != null)
            {
                // The collider scales with the transform, so no need to manually adjust
            }
        }
    }

    public void SetDifficulty(int level)
    {
        currentDifficulty = Mathf.Clamp(level, 0, 2);

        if (difficultySlider != null)
        {
            difficultySlider.value = currentDifficulty;
        }

        UpdateDifficultyDisplay();
        ApplyDifficulty();
    }

    public int GetDifficulty() => currentDifficulty;
    public string GetDifficultyName() => difficultyNames[currentDifficulty];

    public float GetBallScale()
    {
        switch (currentDifficulty)
        {
            case 0: return easyBallScale;
            case 2: return hardBallScale;
            default: return mediumBallScale;
        }
    }
}
