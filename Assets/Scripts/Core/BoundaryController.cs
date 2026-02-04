using UnityEngine;

/// <summary>
/// Attached to ceiling and floor boundaries.
/// Triggers game over when player collides (unless shielded).
/// Tag this GameObject as "Boundary".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoundaryController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Is this the ceiling (true) or floor (false)?")]
    [SerializeField] private bool isCeiling = false;

    void Awake()
    {
        // Ensure this has the Boundary tag
        if (!gameObject.CompareTag("Boundary"))
        {
            Debug.LogWarning($"BoundaryController: GameObject '{gameObject.name}' should be tagged 'Boundary'");
        }

        // Ensure collider is NOT a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // The BallController handles the actual game over logic
        // This script is mainly for organization and potential future expansion
        Debug.Log($"BoundaryController: Player hit {(isCeiling ? "ceiling" : "floor")}");
    }
}
