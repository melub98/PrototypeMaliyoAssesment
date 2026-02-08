using UnityEngine;

/// <summary>
/// Attached to hoop rim/edge colliders.
///
/// DESIGN DECISIONS:
/// - Detects when player touches the edges while passing through
/// - Touching edges doesn't cause game over, but resets multiplier
/// - Must be a child of a GameObject with HoopController
/// - Ball physics (bouncing, rolling, rotation) are handled entirely by
///   Unity's physics engine and the physics material on the colliders
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HoopEdgeCollider : MonoBehaviour
{
    // Reference to parent controller
    private HoopController hoopController;

    void Awake()
    {
        hoopController = GetComponentInParent<HoopController>();
        if (hoopController == null)
        {
            Debug.LogError("HoopEdgeCollider: No HoopController found in parent!");
        }

        // Tag for fast collision checks (avoids GetComponent in BallController)
        gameObject.tag = "HoopEdge";

        // Ensure collider is NOT a trigger (physical collision needed)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Mark that player touched the edge (affects multiplier/scoring)
        hoopController?.OnPlayerTouchEdge();
    }
}
