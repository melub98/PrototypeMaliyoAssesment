using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Controls the player's ball in Flappy Jump.
/// Ball maintains fixed X position (moves with screen scroll).
/// Handles jump mechanics, physics, and shield integration.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Movement")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Position Lock")]
    [Tooltip("X position where ball stays (moves with screen)")]
    [SerializeField] private float lockedXPosition = -3f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float maxRotation = 45f;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip shieldBlockSound;

    [Header("Visual")]
    [SerializeField] private GameObject shieldVisual;

    #endregion

    #region Private Fields

    private Rigidbody2D rb;
    private AudioSource audioSource;

    private InputAction jumpAction;
    private InputAction touchAction;

    private bool isDead = false;
    private bool hasShield = false;
    private Vector3 startPosition;
    private bool jumpPressed = false;

    #endregion

    #region Properties

    public bool HasShield
    {
        get => hasShield;
        set
        {
            hasShield = value;
            UpdateShieldVisual();
        }
    }

    public bool IsDead => isDead;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        startPosition = transform.position;
        lockedXPosition = startPosition.x;
        SetupInputActions();
    }

    void Start()
    {
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.AddListener(OnGameStart);
            GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        }

        UpdateShieldVisual();
    }

    void OnEnable()
    {
        jumpAction?.Enable();
        touchAction?.Enable();
    }

    void OnDisable()
    {
        jumpAction?.Disable();
        touchAction?.Disable();
    }

    void Update()
    {
        if (jumpPressed)
        {
            jumpPressed = false;
            HandleInput();
        }

        if (GameManager.Instance != null && GameManager.Instance.IsPlaying && !isDead)
        {
            HandleRotation();
            LockHorizontalPosition();
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || isDead) return;

        // Extra gravity when falling
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
            GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        }

        jumpAction?.Dispose();
        touchAction?.Dispose();
    }

    #endregion

    #region Input

    void SetupInputActions()
    {
        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.performed += OnJumpPerformed;

        touchAction = new InputAction("Touch", binding: "<Mouse>/leftButton");
        touchAction.AddBinding("<Touchscreen>/primaryTouch/tap");
        touchAction.performed += OnTouchPerformed;

        jumpAction.Enable();
        touchAction.Enable();
    }

    void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    void OnTouchPerformed(InputAction.CallbackContext context)
    {
        if (IsPointerOverUI()) return;
        jumpPressed = true;
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = mousePos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            if (results.Count > 0) return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = touchPos };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            if (results.Count > 0) return true;
        }

        return false;
    }

    void HandleInput()
    {
        if (isDead) return;

        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            Jump();
        }
    }

    #endregion

    #region Movement

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        PlaySound(jumpSound);
    }

    void HandleRotation()
    {
        float targetRotation = Mathf.Clamp(rb.linearVelocity.y * rotationSpeed, -maxRotation, maxRotation);
        transform.rotation = Quaternion.Euler(0, 0, targetRotation);
    }

    /// <summary>
    /// Keeps ball at fixed X position so it moves with the screen scroll.
    /// </summary>
    void LockHorizontalPosition()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x - lockedXPosition) > 0.01f)
        {
            pos.x = lockedXPosition;
            transform.position = pos;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    #endregion

    #region Game State

    void OnGameStart()
    {
        rb.gravityScale = 1;
        isDead = false;
        hasShield = false;
        UpdateShieldVisual();

        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;

        Jump();
    }

    void OnGameOver()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
    }

    #endregion

    #region Collision

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (collision.gameObject.CompareTag("Boundary"))
        {
            HandleBoundaryHit();
        }
    }

    void HandleBoundaryHit()
    {
        if (hasShield)
        {
            hasShield = false;
            UpdateShieldVisual();
            PlaySound(shieldBlockSound);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.5f);
            Debug.Log("BallController: Shield absorbed boundary hit");
            return;
        }

        PlaySound(deathSound);
        GameManager.Instance?.GameOver();
    }

    #endregion

    #region Shield

    public void GrantShield()
    {
        hasShield = true;
        UpdateShieldVisual();
        Debug.Log("BallController: Shield granted");
    }

    public bool UseShield()
    {
        if (hasShield)
        {
            hasShield = false;
            UpdateShieldVisual();
            PlaySound(shieldBlockSound);
            return true;
        }
        return false;
    }

    void UpdateShieldVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(hasShield);
        }
    }

    #endregion

    #region Audio

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Public Methods

    public void ResetBall()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        isDead = false;
        hasShield = false;
        UpdateShieldVisual();
    }

    #endregion
}
