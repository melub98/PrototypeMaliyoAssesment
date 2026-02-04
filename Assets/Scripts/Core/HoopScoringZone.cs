using UnityEngine;

/// <summary>
/// Trigger collider covering the hoop opening.
/// Detects when player enters and exits to track successful passes.
/// Must be a child of a GameObject with HoopController.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HoopScoringZone : MonoBehaviour
{
    private HoopController hoopController;

    void Awake()
    {
        // Get HoopController from parent
        hoopController = GetComponentInParent<HoopController>();
        if (hoopController == null)
        {
            Debug.LogError("HoopScoringZone: No HoopController found in parent!");
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
        if (other.CompareTag("Player"))
        {
            hoopController?.OnPlayerEnterZone();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            hoopController?.OnPlayerExitZone();
        }
    }
}
