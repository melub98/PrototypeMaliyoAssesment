using UnityEngine;

/// <summary>
/// Controller for ceiling and floor boundaries.
///
/// DESIGN DECISIONS:
/// - Boundaries define the playable area - hitting them ends the game (unless shielded)
/// - Tagged as "Boundary" for collision detection in BallController
/// - Uses physical colliders (not triggers) for realistic bounces
///
/// BOUNDARY CLOSING FEATURE (Medium/Hard only):
/// - After reaching a score threshold (default 50), boundaries temporarily close in
/// - This adds pressure and increases difficulty mid-game
/// - Boundaries smoothly animate inward, hold briefly, then return to normal
/// - Creates tension and rewards players who survive the squeeze
///
/// ARCHITECTURE:
/// - Subscribes to GameManager events for game state and score changes
/// - Works in pairs (ceiling + floor) but each operates independently
/// - DifficultyManager determines if closing is enabled
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoundaryController : MonoBehaviour
{
    #region Settings

    [Header("Boundary Type")]
    [Tooltip("Is this the ceiling (true) or floor (false)?")]
    [SerializeField] private bool isCeiling = false;

    [Header("Boundary Closing (Medium/Hard)")]
    [Tooltip("Enable boundary closing effect. Only active on Medium/Hard")]
    [SerializeField] private bool enableClosing = true;

    [Tooltip("Score threshold to trigger boundary closing")]
    [SerializeField] private int closingScoreThreshold = 50;

    [Tooltip("How far the boundary moves inward (units)")]
    [SerializeField] private float closingDistance = 1f;

    [Tooltip("How fast boundaries close in (units per second)")]
    [SerializeField] private float closingSpeed = 2f;

    [Tooltip("How long boundaries stay closed (seconds)")]
    [SerializeField] private float holdDuration = 3f;

    [Tooltip("How fast boundaries return to normal (units per second)")]
    [SerializeField] private float openingSpeed = 1.5f;

    [Tooltip("Score interval between closing events (after first trigger)")]
    [SerializeField] private int closingInterval = 30;

    #endregion

    #region Private Fields

    // Position tracking
    private Vector3 originalPosition;
    private Vector3 targetPosition;

    // State machine for closing animation
    private enum ClosingState { Open, Closing, Holding, Opening }
    private ClosingState currentState = ClosingState.Open;

    // Timing
    private float holdTimer = 0f;
    private int lastClosingScore = 0;

    // Cached difficulty check
    private bool isClosingAllowed = false;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity Awake - Setup collider and validate tag.
    ///
    /// TAG VALIDATION: Warns if not tagged as "Boundary".
    /// BallController checks for this tag to trigger game over.
    /// </summary>
    void Awake()
    {
        // Warn if tag is missing
        if (!gameObject.CompareTag("Boundary"))
        {
            Debug.LogWarning($"BoundaryController: GameObject '{gameObject.name}' should be tagged 'Boundary'");
        }

        // Ensure collider is NOT a trigger (physical collision needed)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        // Store original position for closing animation
        originalPosition = transform.position;
        targetPosition = originalPosition;
    }

    /// <summary>
    /// Unity Start - Subscribe to game events.
    /// </summary>
    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
            GameManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
        }
    }

    /// <summary>
    /// Unity Update - Handle boundary closing animation.
    ///
    /// STATE MACHINE:
    /// 1. Open: Normal position, waiting for trigger
    /// 2. Closing: Moving toward player
    /// 3. Holding: Staying closed for duration
    /// 4. Opening: Returning to normal position
    /// </summary>
    void Update()
    {
        // Only animate during gameplay and if closing is allowed
        if (!isClosingAllowed) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        switch (currentState)
        {
            case ClosingState.Closing:
                UpdateClosing();
                break;

            case ClosingState.Holding:
                UpdateHolding();
                break;

            case ClosingState.Opening:
                UpdateOpening();
                break;
        }
    }

    /// <summary>
    /// Unity OnDestroy - Clean up event subscriptions.
    /// </summary>
    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
            GameManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
        }
    }

    #endregion

    #region State Machine Updates

    /// <summary>
    /// Handles the closing state - boundary moving inward.
    ///
    /// Movement direction depends on whether this is ceiling or floor.
    /// Ceiling moves down, floor moves up.
    /// </summary>
    void UpdateClosing()
    {
        // Move toward target position
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            closingSpeed * Time.deltaTime
        );

        // Check if we've reached the target
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            currentState = ClosingState.Holding;
            holdTimer = holdDuration;
            Debug.Log($"BoundaryController: {(isCeiling ? "Ceiling" : "Floor")} reached closed position");
        }
    }

    /// <summary>
    /// Handles the holding state - boundary stays closed.
    ///
    /// Counts down hold timer then transitions to opening.
    /// </summary>
    void UpdateHolding()
    {
        holdTimer -= Time.deltaTime;

        if (holdTimer <= 0f)
        {
            // Start opening
            targetPosition = originalPosition;
            currentState = ClosingState.Opening;
            Debug.Log($"BoundaryController: {(isCeiling ? "Ceiling" : "Floor")} starting to open");
        }
    }

    /// <summary>
    /// Handles the opening state - boundary returning to normal.
    ///
    /// Moves back to original position at opening speed.
    /// </summary>
    void UpdateOpening()
    {
        // Move toward original position
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            openingSpeed * Time.deltaTime
        );

        // Check if we've reached the original position
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = originalPosition;
            currentState = ClosingState.Open;
            Debug.Log($"BoundaryController: {(isCeiling ? "Ceiling" : "Floor")} fully open");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called when game starts.
    ///
    /// DIFFICULTY CHECK: Boundary closing only active on Medium (1) and Hard (2).
    /// Easy mode (0) keeps boundaries at fixed positions for beginners.
    /// </summary>
    void OnGameStart()
    {
        // Reset to original position
        transform.position = originalPosition;
        currentState = ClosingState.Open;
        lastClosingScore = 0;

        // Check if closing should be enabled based on difficulty
        // Only Medium (1) and Hard (2) get boundary closing
        if (DifficultyManager.Instance != null)
        {
            int difficulty = DifficultyManager.Instance.GetDifficulty();
            isClosingAllowed = enableClosing && difficulty >= 1; // Medium or Hard
        }
        else
        {
            isClosingAllowed = enableClosing;
        }

        Debug.Log($"BoundaryController: Game started, closing allowed = {isClosingAllowed}");
    }

    /// <summary>
    /// Called when game ends.
    ///
    /// Resets boundary to original position for next game.
    /// </summary>
    void OnGameOver()
    {
        // Reset position immediately (no animation)
        transform.position = originalPosition;
        currentState = ClosingState.Open;
    }

    /// <summary>
    /// Called when score changes.
    ///
    /// CLOSING TRIGGER LOGIC:
    /// 1. First closing at threshold (default 50)
    /// 2. Subsequent closings at regular intervals after that
    /// 3. Only triggers if boundaries are currently open
    /// </summary>
    void OnScoreChanged(int newScore)
    {
        // Skip if closing not allowed or already animating
        if (!isClosingAllowed) return;
        if (currentState != ClosingState.Open) return;

        // Check if we should trigger closing
        bool shouldClose = false;

        if (lastClosingScore == 0)
        {
            // First closing - check against initial threshold
            if (newScore >= closingScoreThreshold)
            {
                shouldClose = true;
                lastClosingScore = newScore;
            }
        }
        else
        {
            // Subsequent closings - check against interval from last closing
            if (newScore >= lastClosingScore + closingInterval)
            {
                shouldClose = true;
                lastClosingScore = newScore;
            }
        }

        if (shouldClose)
        {
            TriggerClosing();
        }
    }

    #endregion

    #region Closing Trigger

    /// <summary>
    /// Triggers the boundary closing animation.
    ///
    /// DIRECTION:
    /// - Ceiling moves DOWN (negative Y) toward player
    /// - Floor moves UP (positive Y) toward player
    /// This squeezes the playable area from both sides.
    /// </summary>
    void TriggerClosing()
    {
        // Calculate target position based on boundary type
        if (isCeiling)
        {
            // Ceiling moves down
            targetPosition = originalPosition + Vector3.down * closingDistance;
        }
        else
        {
            // Floor moves up
            targetPosition = originalPosition + Vector3.up * closingDistance;
        }

        currentState = ClosingState.Closing;
        Debug.Log($"BoundaryController: {(isCeiling ? "Ceiling" : "Floor")} starting to close!");
    }

    #endregion

    #region Collision

    /// <summary>
    /// Called when something collides with this boundary.
    ///
    /// NOTE: Actual game over logic is in BallController.
    /// This script only logs the collision for debugging.
    /// Keeping collision handling in one place (BallController) prevents issues.
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Just log - BallController handles the actual game over
        Debug.Log($"BoundaryController: Player hit {(isCeiling ? "ceiling" : "floor")}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually triggers boundary closing.
    /// Can be called from other scripts for special events.
    /// </summary>
    public void ForceClose()
    {
        if (currentState == ClosingState.Open)
        {
            TriggerClosing();
        }
    }

    /// <summary>
    /// Immediately resets boundary to original position.
    /// Used when restarting game without reloading scene.
    /// </summary>
    public void ResetBoundary()
    {
        transform.position = originalPosition;
        currentState = ClosingState.Open;
        lastClosingScore = 0;
    }

    /// <summary>
    /// Returns whether this is the ceiling boundary.
    /// </summary>
    public bool IsCeiling => isCeiling;

    /// <summary>
    /// Returns the current closing state for debugging.
    /// </summary>
    public string GetCurrentState() => currentState.ToString();

    #endregion
}
