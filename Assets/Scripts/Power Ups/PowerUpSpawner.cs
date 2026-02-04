using UnityEngine;

/// <summary>
/// Spawns power-ups at random intervals when platforms are created.
/// Works in conjunction with PlatformSpawner to place power-ups in the gap.
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    // Singleton instance
    public static PowerUpSpawner Instance { get; private set; }

    [Header("Power-Up Prefabs")]
    [Tooltip("Array of power-up prefabs that can be spawned")]
    [SerializeField] private GameObject[] powerUpPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Percentage chance to spawn a power-up with each platform (0-100)")]
    [Range(0f, 100f)]
    [SerializeField] private float spawnChance = 15f;
    [Tooltip("Minimum time between power-up spawns")]
    [SerializeField] private float minSpawnInterval = 5f;

    [Header("Position Settings")]
    [Tooltip("Vertical offset variance for power-up position")]
    [SerializeField] private float verticalVariance = 1f;

    // Tracking
    private float lastSpawnTime = 0f;
    private bool isActive = false;

    /// <summary>
    /// Unity Awake - sets up singleton.
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Unity Start - subscribes to game events.
    /// </summary>
    void Start()
    {
        GameManager.Instance.OnGameStart.AddListener(OnGameStart);
        GameManager.Instance.OnGameOver.AddListener(OnGameOver);
    }

    /// <summary>
    /// Called when game starts - enables spawning.
    /// </summary>
    void OnGameStart()
    {
        isActive = true;
        lastSpawnTime = Time.time;
    }

    /// <summary>
    /// Called when game ends - disables spawning.
    /// </summary>
    void OnGameOver()
    {
        isActive = false;
    }

    /// <summary>
    /// Attempts to spawn a power-up at the given position.
    /// Called by PlatformSpawner when a new platform pair is created.
    /// </summary>
    /// <param name="xPosition">X position for the power-up</param>
    /// <param name="gapCenterY">Y position of the gap center</param>
    public void TrySpawnPowerUp(float xPosition, float gapCenterY)
    {
        if (!isActive) return;
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;

        // Check minimum spawn interval
        if (Time.time - lastSpawnTime < minSpawnInterval) return;

        // Roll for spawn chance
        float roll = Random.Range(0f, 100f);
        if (roll > spawnChance) return;

        // Select random power-up type
        int index = Random.Range(0, powerUpPrefabs.Length);
        GameObject prefab = powerUpPrefabs[index];
        if (prefab == null) return;

        // Calculate spawn position with variance
        float yOffset = Random.Range(-verticalVariance, verticalVariance);
        Vector3 spawnPosition = new Vector3(xPosition, gapCenterY + yOffset, 0f);

        // Spawn the power-up
        Instantiate(prefab, spawnPosition, Quaternion.identity);

        // Update last spawn time
        lastSpawnTime = Time.time;
    }

    /// <summary>
    /// Sets the spawn chance dynamically.
    /// </summary>
    /// <param name="chance">Spawn chance percentage (0-100)</param>
    public void SetSpawnChance(float chance)
    {
        spawnChance = Mathf.Clamp(chance, 0f, 100f);
    }

    /// <summary>
    /// Sets the minimum spawn interval dynamically.
    /// </summary>
    /// <param name="interval">Minimum time between spawns in seconds</param>
    public void SetMinSpawnInterval(float interval)
    {
        minSpawnInterval = Mathf.Max(0f, interval);
    }
}
