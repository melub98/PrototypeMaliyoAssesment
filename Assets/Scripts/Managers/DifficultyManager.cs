using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages game difficulty settings for Flappy Jump.
/// Controls: game speed, hoop spawn rate, hoop size (smaller = harder).
/// Ball size stays constant - only hoop size changes.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TextMeshProUGUI difficultyText;

    [Header("Easy Settings (Larger Hoops)")]
    [SerializeField] private float easyGameSpeed = 2.5f;
    [SerializeField] private float easySpawnInterval = 2.5f;
    [SerializeField] private float easyHoopScale = 0.6f;

    [Header("Medium Settings")]
    [SerializeField] private float mediumGameSpeed = 3.5f;
    [SerializeField] private float mediumSpawnInterval = 2f;
    [SerializeField] private float mediumHoopScale = 0.5f;

    [Header("Hard Settings (Smaller Hoops)")]
    [SerializeField] private float hardGameSpeed = 4.5f;
    [SerializeField] private float hardSpawnInterval = 1.5f;
    [SerializeField] private float hardHoopScale = 0.4f;

    private int currentDifficulty = 1;
    private readonly string[] difficultyNames = { "Easy", "Medium", "Hard" };

    public int CurrentDifficulty => currentDifficulty;

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
        float gameSpeed, spawnInterval;

        switch (currentDifficulty)
        {
            case 0: // Easy
                gameSpeed = easyGameSpeed;
                spawnInterval = easySpawnInterval;
                break;
            case 2: // Hard
                gameSpeed = hardGameSpeed;
                spawnInterval = hardSpawnInterval;
                break;
            default: // Medium
                gameSpeed = mediumGameSpeed;
                spawnInterval = mediumSpawnInterval;
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
            HoopSpawner.Instance.SetHoopScale(GetHoopScale());
        }

        Debug.Log($"Difficulty: {difficultyNames[currentDifficulty]} - Speed={gameSpeed}, Interval={spawnInterval}, HoopScale={GetHoopScale()}");
    }

    public float GetHoopScale()
    {
        switch (currentDifficulty)
        {
            case 0: return easyHoopScale;
            case 2: return hardHoopScale;
            default: return mediumHoopScale;
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
}
