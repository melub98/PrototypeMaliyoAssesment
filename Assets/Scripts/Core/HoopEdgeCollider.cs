using UnityEngine;

/// <summary>
/// Attached to hoop rim/edge colliders.
/// Detects when player touches the edges while passing through.
/// Touching edges doesn't cause game over, but resets multiplier.
/// Must be a child of a GameObject with HoopController.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HoopEdgeCollider : MonoBehaviour
{
    private HoopController hoopController;

    void Awake()
    {
        // Get HoopController from parent
        hoopController = GetComponentInParent<HoopController>();
        if (hoopController == null)
        {
            Debug.LogError("HoopEdgeCollider: No HoopController found in parent!");
        }

        // Ensure collider is NOT a trigger (physical collision)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Mark that player touched the edge
            hoopController?.OnPlayerTouchEdge();

            // Note: We don't trigger game over here
            // Touching while passing through is OK, just affects multiplier
            Debug.Log("HoopEdgeCollider: Player touched hoop edge");
        }
    }
}
