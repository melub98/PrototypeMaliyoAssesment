using UnityEngine;

/// <summary>
/// Spawns basketball hoops at regular intervals.
/// Hoops spawn at random angles and positions.
/// Progressive difficulty: moving hoops appear after certain score thresholds.
/// </summary>
public class HoopSpawner : MonoBehaviour
{
    public static HoopSpawner Instance { get; private set; }

    [Header("Hoop Prefab")]
    [Tooltip("The hoop prefab to spawn")]
    [SerializeField] private GameObject hoopPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Time between hoop spawns")]
    [SerializeField] private float spawnInterval = 2f;

    [Tooltip("X position where hoops spawn")]
    [SerializeField] private float spawnXPosition = 12f;

    [Header("Position Settings")]
    [Tooltip("Minimum Y position for hoop")]
    [SerializeField] private float minY = -2f;

    [Tooltip("Maximum Y position for hoop")]
    [SerializeField] private float maxY = 2f;

    [Header("Rotation Settings")]
    [Tooltip("Possible rotation angles for hoops (horizontal = 0)")]
    [SerializeField] private float[] easyRotations = { -15f, 0f, 15f };
    [SerializeField] private float[] mediumRotations = { -30f, -15f, 0f, 15f, 30f };
    [SerializeField] private float[] hardRotations = { -45f, -30f, -15f, 0f, 15f, 30f, 45f };

    [Header("Progressive Difficulty")]
    [Tooltip("Score threshold when moving hoops start appearing")]
    [SerializeField] private int movingHoopScoreThreshold = 10;

    [Tooltip("Chance for moving hoop after threshold (0-100)")]
    [SerializeField] private float movingHoopChance = 30f;

    [Tooltip("Movement range for moving hoops")]
    [SerializeField] private float hoopMoveRange = 1f;

    [Tooltip("Movement speed for moving hoops")]
    [SerializeField] private float hoopMoveSpeed = 1.5f;

    [Header("Shield Power-Up")]
    [Tooltip("Shield power-up prefab")]
    [SerializeField] private GameObject shieldPrefab;

    [Tooltip("Chance to spawn shield (0-100)")]
    [Range(0f, 100f)]
    [SerializeField] private float shieldSpawnChance = 15f;

    [Tooltip("Minimum time between shield spawns")]
    [SerializeField] private float minShieldInterval = 8f;

    // State
    private float spawnTimer = 0f;
    private float lastShieldSpawnTime = 0f;
    private bool isSpawning = false;
    private int currentDifficulty = 1;
    private int hoopsSpawned = 0;

    // Base values
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

        // Get current difficulty
        if (DifficultyManager.Instance != null)
        {
            currentDifficulty = DifficultyManager.Instance.GetDifficulty();
        }

        Debug.Log("HoopSpawner: Started spawning hoops");
    }

    void OnGameOver()
    {
        isSpawning = false;
    }

    void SpawnHoop()
    {
        if (hoopPrefab == null) return;

        // Random Y position
        float yPos = Random.Range(minY, maxY);

        // Get rotation based on difficulty
        float angle = GetRandomRotation();

        // Spawn position and rotation
        Vector3 spawnPos = new Vector3(spawnXPosition, yPos, 0f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // Instantiate hoop
        GameObject hoop = Instantiate(hoopPrefab, spawnPos, rotation);
        hoop.name = "Hoop";

        // Check if this hoop should move (progressive difficulty)
        HoopController hoopController = hoop.GetComponent<HoopController>();
        if (hoopController != null && ShouldHoopMove())
        {
            hoopController.SetMovementEnabled(true);
            hoopController.SetMovementParams(hoopMoveRange, hoopMoveSpeed);
        }

        hoopsSpawned++;

        // Try to spawn shield power-up
        TrySpawnShield(spawnPos);

        Debug.Log($"HoopSpawner: Spawned hoop #{hoopsSpawned} at Y={yPos:F1}, angle={angle}°");
    }

    float GetRandomRotation()
    {
        float[] rotations;

        switch (currentDifficulty)
        {
            case 0: // Easy
                rotations = easyRotations;
                break;
            case 2: // Hard
                rotations = hardRotations;
                break;
            default: // Medium
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

        // Only start moving hoops after threshold
        if (currentScore < movingHoopScoreThreshold) return false;

        // Random chance
        return Random.Range(0f, 100f) < movingHoopChance;
    }

    void TrySpawnShield(Vector3 hoopPos)
    {
        if (shieldPrefab == null) return;

        if (Time.time - lastShieldSpawnTime < minShieldInterval) return;

        if (Random.Range(0f, 100f) > shieldSpawnChance) return;

        float offsetY = Random.Range(-1f, 1f);
        float offsetX = Random.Range(-1f, 1f);
        Vector3 shieldPos = hoopPos + new Vector3(offsetX, offsetY, 0f);

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

    public void ResetToBaseValues()
    {
        spawnInterval = baseSpawnInterval;
    }

    public float GetSpawnInterval() => spawnInterval;

    #endregion
}
