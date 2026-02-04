using UnityEngine;

/// <summary>
/// Trigger zone placed behind/after each hoop.
/// Detects if player passed the hoop without going through the opening.
/// Missing a hoop = game over.
/// Must be a child of a GameObject with HoopController.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MissDetector : MonoBehaviour
{
    private HoopController hoopController;
    private bool triggered = false;

    void Awake()
    {
        // Get HoopController from parent
        hoopController = GetComponentInParent<HoopController>();
        if (hoopController == null)
        {
            Debug.LogError("MissDetector: No HoopController found in parent!");
        }

        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Check if player entered the hoop zone
            if (hoopController != null && !hoopController.playerEnteredZone && !hoopController.hoopCleared)
            {
                // Player missed the hoop!
                hoopController.OnPlayerMissedHoop();
            }
        }
    }
}
