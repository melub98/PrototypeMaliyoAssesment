using UnityEngine;

/// <summary>
/// Spawns pairs of horizontal platforms (top + bottom) with gaps at regular intervals.
/// The gap between platforms is where the player must navigate through.
/// </summary>
public class PlatformSpawner : MonoBehaviour
{
    [Header("Platform Prefabs")]
    [Tooltip("Prefab for the top platform section")]
    [SerializeField] private GameObject topPlatformPrefab;
    [Tooltip("Prefab for the bottom platform section")]
    [SerializeField] private GameObject bottomPlatformPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Time between platform pair spawns")]
    [SerializeField] private float spawnInterval = 2f;
    [Tooltip("X position where platforms spawn (off-screen right)")]
    [SerializeField] private float spawnXPosition = 12f;

    [Header("Gap Settings")]
    [Tooltip("Vertical size of the gap between platforms")]
    [SerializeField] private float gapSize = 3f;
    [Tooltip("Minimum Y position for gap center")]
    [SerializeField] private float minGapY = -2f;
    [Tooltip("Maximum Y position for gap center")]
    [SerializeField] private float maxGapY = 2f;

    [Header("Platform Dimensions")]
    [Tooltip("Height of the screen/play area")]
    [SerializeField] private float screenHeight = 10f;

    // Timer for spawn interval
    private float spawnTimer = 0f;
    // Controls whether spawning is active
    private bool isSpawning = false;

    /// <summary>
    /// Unity Start - subscribes to game events.
    /// </summary>
    void Start()
    {
        // Start spawning when game begins
        GameManager.Instance.OnGameStart.AddListener(OnGameStart);
        // Stop spawning when game ends
        GameManager.Instance.OnGameOver.AddListener(OnGameOver);
    }

    /// <summary>
    /// Unity Update - handles spawn timing.
    /// </summary>
    void Update()
    {
        if (!isSpawning) return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            SpawnPlatformPair();
            spawnTimer = 0f;
        }
    }

    /// <summary>
    /// Called when game starts - enables spawning.
    /// </summary>
    void OnGameStart()
    {
        isSpawning = true;
        spawnTimer = spawnInterval * 0.5f; // Spawn first platform sooner
    }

    /// <summary>
    /// Called when game ends - disables spawning.
    /// </summary>
    void OnGameOver()
    {
        isSpawning = false;
    }

    /// <summary>
    /// Spawns a pair of platforms (top and bottom) with a gap between them.
    /// </summary>
    void SpawnPlatformPair()
    {
        // Randomize gap center position
        float gapCenterY = Random.Range(minGapY, maxGapY);

        // Calculate platform positions based on gap
        float topPlatformY = gapCenterY + (gapSize / 2f) + (screenHeight / 4f);
        float bottomPlatformY = gapCenterY - (gapSize / 2f) - (screenHeight / 4f);

        // Spawn top platform
        Vector3 topPosition = new Vector3(spawnXPosition, topPlatformY, 0);
        GameObject topPlatform = Instantiate(topPlatformPrefab, topPosition, Quaternion.identity);
        topPlatform.name = "TopPlatform";

        // Spawn bottom platform
        Vector3 bottomPosition = new Vector3(spawnXPosition, bottomPlatformY, 0);
        GameObject bottomPlatform = Instantiate(bottomPlatformPrefab, bottomPosition, Quaternion.identity);
        bottomPlatform.name = "BottomPlatform";

        // Notify PowerUpSpawner about new platform pair for potential power-up spawn
        if (PowerUpSpawner.Instance != null)
        {
            PowerUpSpawner.Instance.TrySpawnPowerUp(spawnXPosition, gapCenterY);
        }
    }

    /// <summary>
    /// Gets the current gap size (useful for other systems).
    /// </summary>
    public float GetGapSize() => gapSize;

    /// <summary>
    /// Sets the spawn interval dynamically (for difficulty scaling).
    /// </summary>
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.5f, interval);
    }

    /// <summary>
    /// Sets the gap size dynamically (for difficulty scaling).
    /// </summary>
    public void SetGapSize(float size)
    {
        gapSize = Mathf.Max(1.5f, size);
    }
}
