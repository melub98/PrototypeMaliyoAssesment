using UnityEngine;
using System.Collections;

/// <summary>
/// Animates wing sprites that follow the ball without inheriting its rotation.
/// Wings are NOT children of the ball - they are separate scene objects.
/// This script (on the Ball) moves them to follow the ball's position each frame.
/// </summary>
public class WingFlap : MonoBehaviour
{
    [Header("Wing References (NOT children of ball)")]
    [Tooltip("Left/front wing - a separate GameObject in the scene")]
    [SerializeField] private Transform leftWing;

    [Tooltip("Right/back wing - a separate GameObject in the scene")]
    [SerializeField] private Transform rightWing;

    [Header("Position Offsets")]
    [Tooltip("Left wing offset from ball center")]
    [SerializeField] private Vector3 leftWingOffset = new Vector3(-0.3f, 0f, 0f);

    [Tooltip("Right wing offset from ball center")]
    [SerializeField] private Vector3 rightWingOffset = new Vector3(0.3f, 0f, 0f);

    [Header("Flap Settings")]
    [Tooltip("How far wings rotate up on flap (degrees)")]
    [SerializeField] private float flapUpAngle = 60f;

    [Tooltip("Resting angle for wings (degrees, slight droop)")]
    [SerializeField] private float restAngle = -20f;

    [Tooltip("How fast wings flap up (seconds)")]
    [SerializeField] private float flapUpDuration = 0.05f;

    [Tooltip("How fast wings ease back down (seconds)")]
    [SerializeField] private float flapDownDuration = 0.25f;

    private Coroutine flapCoroutine;
    private float currentFlapAngle;

    void Start()
    {
        currentFlapAngle = restAngle;

        // Unparent wings so they don't inherit ball rotation
        if (leftWing != null && leftWing.parent == transform)
            leftWing.SetParent(null);
        if (rightWing != null && rightWing.parent == transform)
            rightWing.SetParent(null);

        ApplyWings();
    }

    void LateUpdate()
    {
        ApplyWings();
    }

    /// <summary>
    /// Triggers a wing flap. Called from BallController on jump.
    /// </summary>
    public void Flap()
    {
        if (leftWing == null && rightWing == null) return;

        if (flapCoroutine != null)
            StopCoroutine(flapCoroutine);

        flapCoroutine = StartCoroutine(FlapAnimation());
    }

    IEnumerator FlapAnimation()
    {
        float elapsed = 0f;
        float startAngle = currentFlapAngle;

        // Flap up quickly
        while (elapsed < flapUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flapUpDuration;
            currentFlapAngle = Mathf.Lerp(startAngle, flapUpAngle, t);
            yield return null;
        }

        currentFlapAngle = flapUpAngle;

        // Ease back down
        elapsed = 0f;
        while (elapsed < flapDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flapDownDuration;
            t = 1f - (1f - t) * (1f - t);
            currentFlapAngle = Mathf.Lerp(flapUpAngle, restAngle, t);
            yield return null;
        }

        currentFlapAngle = restAngle;
        flapCoroutine = null;
    }

    /// <summary>
    /// Positions wings at ball + offset, applies only flap rotation.
    /// No parent-child relationship, so ball spin has zero effect.
    /// </summary>
    void ApplyWings()
    {
        Vector3 ballPos = transform.position;

        if (leftWing != null)
        {
            leftWing.position = ballPos + leftWingOffset;
            leftWing.rotation = Quaternion.Euler(0f, 0f, currentFlapAngle);
        }

        if (rightWing != null)
        {
            rightWing.position = ballPos + rightWingOffset;
            rightWing.rotation = Quaternion.Euler(0f, 0f, currentFlapAngle);
        }
    }
}
