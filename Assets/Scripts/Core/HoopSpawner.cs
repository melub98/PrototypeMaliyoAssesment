using UnityEngine;

/// <summary>
/// Spawns basketball hoops at regular intervals.
/// Supports horizontal (0°) and vertical (90°) hoops plus angled variations.
/// Scales hoops based on difficulty (smaller = harder).
/// </summary>
public class HoopSpawner : MonoBehaviour
{
    public static HoopSpawner Instance { get; private set; }

    [Header("Hoop Prefab")]
    [SerializeField] private GameObject hoopPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnXPosition = 12f;

    [Header("Position Settings")]
    [SerializeField] private float minY = -2f;
    [SerializeField] private float maxY = 2f;

    [Header("Rotation Settings")]
    [Tooltip("Horizontal (0°) and Vertical (90°) plus variations")]
    [SerializeField] private float[] easyRotations = { 0f, 0f, 0f, 15f, -15f };
    [SerializeField] private float[] mediumRotations = { 0f, 90f, 30f, -30f, 45f, -45f };
    [SerializeField] private float[] hardRotations = { 0f, 90f, 45f, -45f, 60f, -60f, 75f, -75f, 90f };

    [Header("Hoop Scaling")]
    [SerializeField] private float currentHoopScale = 1f;

    [Header("Progressive Difficulty")]
    [SerializeField] private int movingHoopScoreThreshold = 10;
    [SerializeField] private float movingHoopChance = 30f;
    [SerializeField] private float hoopMoveRange = 1f;
    [SerializeField] private float hoopMoveSpeed = 1.5f;

    [Header("Shield Power-Up")]
    [SerializeField] private GameObject shieldPrefab;
    [Tooltip("Higher chance for power-ups (0-100)")]
    [Range(0f, 100f)]
    [SerializeField] private float shieldSpawnChance = 30f;
    [SerializeField] private float minShieldInterval = 5f;

    // State
    private float spawnTimer = 0f;
    private float lastShieldSpawnTime = 0f;
    private bool isSpawning = false;
    private int currentDifficulty = 1;
    private int hoopsSpawned = 0;
    private float baseSpawnInterval;

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

        baseSpawnInterval = spawnInterval;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }

        if (hoopPrefab == null)
        {
            Debug.LogError("HoopSpawner: No hoop prefab assigned!");
        }
    }

    void Update()
    {
        if (!isSpawning) return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            SpawnHoop();
            spawnTimer = 0f;
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

    void OnGameStart()
    {
        isSpawning = true;
        spawnTimer = spawnInterval * 0.5f;
        lastShieldSpawnTime = Time.time;
        hoopsSpawned = 0;

        if (DifficultyManager.Instance != null)
        {
            currentDifficulty = DifficultyManager.Instance.GetDifficulty();
            currentHoopScale = DifficultyManager.Instance.GetHoopScale();
        }

        Debug.Log($"HoopSpawner: Started - Difficulty={currentDifficulty}, HoopScale={currentHoopScale}");
    }

    void OnGameOver()
    {
        isSpawning = false;
    }

    void SpawnHoop()
    {
        if (hoopPrefab == null) return;

        float yPos = Random.Range(minY, maxY);
        float angle = GetRandomRotation();

        Vector3 spawnPos = new Vector3(spawnXPosition, yPos, 0f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        GameObject hoop = Instantiate(hoopPrefab, spawnPos, rotation);
        hoop.name = "Hoop";

        // Apply scale based on difficulty
        hoop.transform.localScale = Vector3.one * currentHoopScale;

        // Check if this hoop should move
        HoopController hoopController = hoop.GetComponent<HoopController>();
        if (hoopController != null && ShouldHoopMove())
        {
            hoopController.SetMovementEnabled(true);
            hoopController.SetMovementParams(hoopMoveRange, hoopMoveSpeed);
        }

        hoopsSpawned++;

        // Try to spawn shield (more frequent now)
        TrySpawnShield(spawnPos);

        Debug.Log($"HoopSpawner: Spawned hoop #{hoopsSpawned} at Y={yPos:F1}, angle={angle}°, scale={currentHoopScale}");
    }

    float GetRandomRotation()
    {
        float[] rotations;

        switch (currentDifficulty)
        {
            case 0:
                rotations = easyRotations;
                break;
            case 2:
                rotations = hardRotations;
                break;
            default:
                rotations = mediumRotations;
                break;
        }

        if (rotations != null && rotations.Length > 0)
        {
            return rotations[Random.Range(0, rotations.Length)];
        }

        return 0f;
    }

    bool ShouldHoopMove()
    {
        if (GameManager.Instance == null) return false;

        int currentScore = GameManager.Instance.GetScore();
        if (currentScore < movingHoopScoreThreshold) return false;

        return Random.Range(0f, 100f) < movingHoopChance;
    }

    void TrySpawnShield(Vector3 hoopPos)
    {
        if (shieldPrefab == null) return;

        if (Time.time - lastShieldSpawnTime < minShieldInterval) return;

        if (Random.Range(0f, 100f) > shieldSpawnChance) return;

        float offsetY = Random.Range(-1f, 1f);
        float offsetX = Random.Range(0.5f, 2f);
        Vector3 shieldPos = hoopPos + new Vector3(-offsetX, offsetY, 0f);

        Instantiate(shieldPrefab, shieldPos, Quaternion.identity);
        lastShieldSpawnTime = Time.time;

        Debug.Log("HoopSpawner: Spawned shield power-up");
    }

    #region Public Methods

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.5f, interval);
    }

    public void SetDifficulty(int difficulty)
    {
        currentDifficulty = Mathf.Clamp(difficulty, 0, 2);
    }

    public void SetHoopScale(float scale)
    {
        currentHoopScale = Mathf.Clamp(scale, 0.5f, 2f);
    }

    public void ResetToBaseValues()
    {
        spawnInterval = baseSpawnInterval;
    }

    public float GetSpawnInterval() => spawnInterval;

    #endregion
}
