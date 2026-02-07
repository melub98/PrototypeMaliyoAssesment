using UnityEngine;

/// <summary>
/// Spawns basketball hoops at regular intervals during gameplay.
///
/// DESIGN DECISIONS:
/// - Hoops spawn off-screen right and move left (world scrolls past player)
/// - Spawn interval and hoop size vary by difficulty level
/// - Random Y position within bounds creates varied patterns
/// - Supports angled hoops for increased difficulty
///
/// PROGRESSIVE DIFFICULTY:
/// - After a score threshold, hoops can oscillate vertically
/// - Harder difficulties have smaller hoops, faster spawns, more rotation variety
/// - Moving hoops have a chance to spawn based on player score
///
/// POWER-UP SPAWNING:
/// - Shield power-ups spawn WITHIN the hoop opening
/// - This encourages players to pass through hoops to collect shields
/// - Spawn chance and interval are configurable
///
/// SINGLETON PATTERN:
/// - Uses static Instance for easy access from DifficultyManager
/// - Only one spawner should exist in the scene
/// </summary>
public class HoopSpawner : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Singleton instance for global access.
    /// DifficultyManager uses this to adjust spawn settings.
    /// </summary>
    public static HoopSpawner Instance { get; private set; }

    #endregion

    #region Serialized Fields

    [Header("Hoop Prefab")]
    [Tooltip("The hoop prefab to spawn. Must have HoopController and HoopMover components")]
    [SerializeField] private GameObject hoopPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Time between hoop spawns in seconds. Adjusted by difficulty")]
    [SerializeField] private float spawnInterval = 2f;

    [Tooltip("X position where hoops spawn (off-screen right)")]
    [SerializeField] private float spawnXPosition = 12f;

    [Header("Position Settings")]
    [Tooltip("Minimum Y position for hoop spawn")]
    [SerializeField] private float minY = -2f;

    [Tooltip("Maximum Y position for hoop spawn")]
    [SerializeField] private float maxY = 2f;

    [Header("Rotation Settings")]
    [Tooltip("Base rotation offset applied to all hoops. Adjust so opening faces up")]
    [SerializeField] private float baseRotationOffset = 0f;

    [Tooltip("Rotation angles for Easy difficulty. Mostly horizontal, slight tilts")]
    [SerializeField] private float[] easyRotations = { 0f, 0f, 0f, 15f, -15f };

    [Tooltip("Rotation angles for Medium difficulty. Mix of horizontal and angled")]
    [SerializeField] private float[] mediumRotations = { 0f, 90f, 30f, -30f, 45f, -45f };

    [Tooltip("Rotation angles for Hard difficulty. Most variety including steep angles")]
    [SerializeField] private float[] hardRotations = { 0f, 90f, 45f, -45f, 60f, -60f, 75f, -75f, 90f };

    [Header("Hoop Scaling")]
    [Tooltip("Current scale applied to spawned hoops. Set by DifficultyManager")]
    [SerializeField] private float currentHoopScale = 1f;

    [Header("Progressive Difficulty")]
    [Tooltip("Score threshold before moving hoops can appear")]
    [SerializeField] private int movingHoopScoreThreshold = 10;

    [Tooltip("Chance (0-100) for a hoop to move after threshold")]
    [SerializeField] private float movingHoopChance = 30f;

    [Tooltip("Vertical movement range for moving hoops")]
    [SerializeField] private float hoopMoveRange = 1f;

    [Tooltip("Movement speed for moving hoops")]
    [SerializeField] private float hoopMoveSpeed = 1.5f;

    [Header("Reverse Hoop")]
    [Tooltip("Reverse hoop prefab (player enters from opposite direction). Must have HoopController and HoopMover")]
    [SerializeField] private GameObject reverseHoopPrefab;

    [Tooltip("Score threshold before reverse hoops can appear")]
    [SerializeField] private int reverseHoopScoreThreshold = 5;

    [Tooltip("Chance (0-100) for a reverse hoop to spawn instead of normal after threshold")]
    [Range(0f, 100f)]
    [SerializeField] private float reverseHoopChance = 25f;

    [Header("Shield Power-Up")]
    [Tooltip("Shield power-up prefab. Spawns within hoops for collection")]
    [SerializeField] private GameObject shieldPrefab;

    [Tooltip("Chance (0-100) for shield to spawn with each hoop")]
    [Range(0f, 100f)]
    [SerializeField] private float shieldSpawnChance = 30f;

    [Tooltip("Minimum time between shield spawns in seconds")]
    [SerializeField] private float minShieldInterval = 5f;

    #endregion

    #region Private Fields

    // Spawn timing
    private float spawnTimer = 0f;
    private float lastShieldSpawnTime = 0f;

    // State
    private bool isSpawning = false;
    private int currentDifficulty = 1; // 0=Easy, 1=Medium, 2=Hard
    private int hoopsSpawned = 0;

    // Base values for reset
    private float baseSpawnInterval;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity Awake - Setup singleton and store initial values.
    ///
    /// SINGLETON: Destroys duplicate instances to ensure only one spawner exists.
    /// This prevents issues with multiple spawners creating too many hoops.
    /// </summary>
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // Duplicate found - destroy this one
            Destroy(gameObject);
            return;
        }

        // Store initial spawn interval for reset functionality
        baseSpawnInterval = spawnInterval;
    }

    /// <summary>
    /// Unity Start - Subscribe to game events and validate prefab.
    ///
    /// VALIDATION: Logs error if hoop prefab isn't assigned.
    /// Game will still run but no hoops will spawn.
    /// </summary>
    void Start()
    {
        // Subscribe to game state events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }

        // Validate required prefabs
        if (hoopPrefab == null)
        {
            Debug.LogError("HoopSpawner: No hoop prefab assigned! Hoops will not spawn.");
        }
        if (reverseHoopPrefab == null)
        {
            Debug.LogWarning("HoopSpawner: No reverse hoop prefab assigned. Reverse hoops will not spawn.");
        }
        if (shieldPrefab == null)
        {
            Debug.LogWarning("HoopSpawner: No shield prefab assigned! Shield power-ups will not spawn. Assign a shield prefab in the Inspector.");
        }
    }

    /// <summary>
    /// Unity Update - Handle spawn timing.
    ///
    /// SPAWN LOGIC: Accumulates delta time until interval is reached,
    /// then spawns a hoop and resets timer.
    /// </summary>
    void Update()
    {
        // Only spawn during active gameplay
        if (!isSpawning) return;

        // Accumulate time
        spawnTimer += Time.deltaTime;

        // Check if it's time to spawn
        if (spawnTimer >= spawnInterval)
        {
            SpawnHoop();
            spawnTimer = 0f;
        }
    }

    /// <summary>
    /// Unity OnDestroy - Clean up event subscriptions.
    ///
    /// IMPORTANT: Always unsubscribe to prevent memory leaks
    /// and errors when GameManager is destroyed first.
    /// </summary>
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }
    }

    #endregion

    #region Game Event Handlers

    /// <summary>
    /// Called when game starts.
    ///
    /// SETUP:
    /// - Enables spawning
    /// - Sets timer to half interval so first hoop appears quickly
    /// - Resets shield spawn timer
    /// - Gets current difficulty settings
    /// </summary>
    void OnGameStart()
    {
        isSpawning = true;

        // Start timer at half interval so first hoop appears sooner
        // Full interval would feel too slow at game start
        spawnTimer = spawnInterval * 0.5f;

        // Reset shield spawn tracking
        lastShieldSpawnTime = Time.time;
        hoopsSpawned = 0;

        // Get difficulty settings
        if (DifficultyManager.Instance != null)
        {
            currentDifficulty = DifficultyManager.Instance.GetDifficulty();
            currentHoopScale = DifficultyManager.Instance.GetHoopScale();
        }

        Debug.Log($"HoopSpawner: Started - Difficulty={currentDifficulty}, HoopScale={currentHoopScale}");
    }

    /// <summary>
    /// Called when game ends.
    /// Stops spawning but doesn't destroy existing hoops.
    /// </summary>
    void OnGameOver()
    {
        isSpawning = false;
    }

    #endregion

    #region Spawning

    /// <summary>
    /// Spawns a new hoop with random position and rotation.
    ///
    /// SPAWN PROCESS:
    /// 1. Generate random Y position within bounds
    /// 2. Select random rotation from difficulty-appropriate array
    /// 3. Apply base rotation offset (for hoop orientation correction)
    /// 4. Instantiate hoop at calculated position/rotation
    /// 5. Scale hoop based on difficulty
    /// 6. Optionally enable movement for progressive difficulty
    /// 7. Attempt to spawn shield power-up within hoop
    /// </summary>
    void SpawnHoop()
    {
        // Validate prefab exists
        if (hoopPrefab == null) return;

        // Determine if this should be a reverse hoop
        bool isReverse = ShouldSpawnReverseHoop();
        GameObject prefabToUse = isReverse && reverseHoopPrefab != null ? reverseHoopPrefab : hoopPrefab;

        // Random Y position within bounds
        float yPos = Random.Range(minY, maxY);

        // Get rotation from difficulty-appropriate array, plus base offset
        float angle = GetRandomRotation() + baseRotationOffset;

        // Calculate spawn position and rotation
        Vector3 spawnPos = new Vector3(spawnXPosition, yPos, 0f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // Instantiate the hoop
        GameObject hoop = Instantiate(prefabToUse, spawnPos, rotation);
        hoop.name = isReverse ? "ReverseHoop" : "Hoop";

        // Apply scale based on difficulty (smaller = harder)
        hoop.transform.localScale = Vector3.one * currentHoopScale;

        // Get HoopController for configuration
        HoopController hoopController = hoop.GetComponent<HoopController>();

        if (hoopController != null)
        {
            // Apply fire effect based on current multiplier streak
            int currentMultiplier = GameManager.Instance != null ?
                GameManager.Instance.CurrentMultiplier : 1;
            hoopController.SetMultiplierVisuals(currentMultiplier);

            // Check if this hoop should move (progressive difficulty)
            if (ShouldHoopMove())
            {
                hoopController.SetMovementEnabled(true);
                hoopController.SetMovementParams(hoopMoveRange, hoopMoveSpeed);
            }
        }

        hoopsSpawned++;

        // Try to spawn shield power-up WITHIN this hoop
        TrySpawnShield(spawnPos, rotation);

        Debug.Log($"HoopSpawner: Spawned hoop #{hoopsSpawned} at Y={yPos:F1}, angle={angle}°, scale={currentHoopScale}");
    }

    /// <summary>
    /// Gets a random rotation angle based on current difficulty.
    ///
    /// DIFFICULTY ROTATION DESIGN:
    /// - Easy: Mostly 0° (horizontal) with slight 15° tilts
    /// - Medium: Mix including 90° (vertical) and moderate angles
    /// - Hard: Full variety including steep 75° angles
    ///
    /// Weighted by array contents - Easy has multiple 0° entries
    /// to make horizontal hoops more common.
    /// </summary>
    float GetRandomRotation()
    {
        float[] rotations;

        // Select rotation array based on difficulty
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

        // Return random angle from array, or 0 if array is empty
        if (rotations != null && rotations.Length > 0)
        {
            return rotations[Random.Range(0, rotations.Length)];
        }

        return 0f;
    }

    /// <summary>
    /// Determines if a hoop should have movement enabled.
    ///
    /// PROGRESSIVE DIFFICULTY:
    /// - Moving hoops only appear after player reaches score threshold
    /// - Even then, only a percentage of hoops move
    /// - This gradually increases difficulty as game progresses
    /// </summary>
    bool ShouldHoopMove()
    {
        if (GameManager.Instance == null) return false;

        // Check if player has reached the threshold
        int currentScore = GameManager.Instance.GetScore();
        if (currentScore < movingHoopScoreThreshold) return false;

        // Random chance for movement
        return Random.Range(0f, 100f) < movingHoopChance;
    }

    /// <summary>
    /// Determines if the next hoop should be a reverse hoop.
    /// Reverse hoops only appear after player reaches score threshold.
    /// </summary>
    bool ShouldSpawnReverseHoop()
    {
        if (reverseHoopPrefab == null) return false;
        if (GameManager.Instance == null) return false;

        int currentScore = GameManager.Instance.GetScore();
        if (currentScore < reverseHoopScoreThreshold) return false;

        return Random.Range(0f, 100f) < reverseHoopChance;
    }

    /// <summary>
    /// Attempts to spawn a shield power-up within the hoop.
    ///
    /// DESIGN DECISION: Shields spawn INSIDE the hoop opening.
    /// This encourages players to pass through hoops to collect shields,
    /// rather than having shields appear randomly in empty space.
    ///
    /// SPAWN CONDITIONS:
    /// 1. Shield prefab must be assigned
    /// 2. Minimum time must have passed since last shield
    /// 3. Random chance must succeed
    /// </summary>
    /// <param name="hoopPos">Position of the hoop</param>
    /// <param name="hoopRotation">Rotation of the hoop</param>
    void TrySpawnShield(Vector3 hoopPos, Quaternion hoopRotation)
    {
        // Validate prefab
        if (shieldPrefab == null) return;

        // Check minimum interval between shields
        if (Time.time - lastShieldSpawnTime < minShieldInterval) return;

        // Random chance check
        if (Random.Range(0f, 100f) > shieldSpawnChance) return;

        // SPAWN WITHIN HOOP: Position shield at hoop center
        // Small random offset keeps it interesting but still inside hoop
        float offsetY = Random.Range(-0.3f, 0.3f);
        float offsetX = Random.Range(-0.2f, 0.2f);

        // Apply offset relative to hoop rotation
        Vector3 offset = hoopRotation * new Vector3(offsetX, offsetY, 0f);
        Vector3 shieldPos = hoopPos + offset;

        // Instantiate shield at calculated position
        Instantiate(shieldPrefab, shieldPos, Quaternion.identity);
        lastShieldSpawnTime = Time.time;

        Debug.Log("HoopSpawner: Spawned shield power-up within hoop");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the spawn interval.
    /// Called by DifficultyManager when difficulty changes.
    /// Minimum 0.5s prevents impossible spawn rates.
    /// </summary>
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.5f, interval);
    }

    /// <summary>
    /// Sets the current difficulty level.
    /// Affects which rotation array is used.
    /// </summary>
    public void SetDifficulty(int difficulty)
    {
        currentDifficulty = Mathf.Clamp(difficulty, 0, 2);
    }

    /// <summary>
    /// Sets the hoop scale.
    /// Smaller hoops = harder game.
    /// Clamped to prevent extreme values.
    /// </summary>
    public void SetHoopScale(float scale)
    {
        currentHoopScale = Mathf.Clamp(scale, 0.3f, 2f);
    }

    /// <summary>
    /// Resets spawn interval to initial value.
    /// Used when restarting game.
    /// </summary>
    public void ResetToBaseValues()
    {
        spawnInterval = baseSpawnInterval;
    }

    /// <summary>
    /// Gets current spawn interval for UI display.
    /// </summary>
    public float GetSpawnInterval() => spawnInterval;

    #endregion
}
