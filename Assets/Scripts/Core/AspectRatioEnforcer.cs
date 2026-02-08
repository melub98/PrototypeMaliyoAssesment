using UnityEngine;

/// <summary>
/// Forces the camera to maintain a 9:16 portrait aspect ratio.
/// Adds black letterbox/pillarbox bars when the window doesn't match.
/// Attach to the Main Camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [Tooltip("Target aspect ratio width (9 for 9:16 portrait)")]
    [SerializeField] private float targetWidth = 9f;

    [Tooltip("Target aspect ratio height (16 for 9:16 portrait)")]
    [SerializeField] private float targetHeight = 16f;

    private Camera mainCamera;
    private Camera letterboxCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();

        // Create a background camera for the black bars
        GameObject letterboxObj = new GameObject("LetterboxCamera");
        letterboxCamera = letterboxObj.AddComponent<Camera>();
        letterboxCamera.depth = mainCamera.depth - 1;
        letterboxCamera.cullingMask = 0;
        letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
        letterboxCamera.backgroundColor = Color.black;
        letterboxCamera.orthographic = true;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        UpdateAspectRatio();
    }

    void Update()
    {
        // Only recalculate when screen size actually changes
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            UpdateAspectRatio();
        }
    }

    void UpdateAspectRatio()
    {
        float targetAspect = targetWidth / targetHeight;
        float windowAspect = (float)Screen.width / Screen.height;

        if (Mathf.Abs(windowAspect - targetAspect) < 0.01f)
        {
            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        if (windowAspect > targetAspect)
        {
            float viewportWidth = targetAspect / windowAspect;
            float x = (1f - viewportWidth) / 2f;
            mainCamera.rect = new Rect(x, 0f, viewportWidth, 1f);
        }
        else
        {
            float viewportHeight = windowAspect / targetAspect;
            float y = (1f - viewportHeight) / 2f;
            mainCamera.rect = new Rect(0f, y, 1f, viewportHeight);
        }
    }

    void OnDestroy()
    {
        if (letterboxCamera != null)
        {
            Destroy(letterboxCamera.gameObject);
        }
    }
}
