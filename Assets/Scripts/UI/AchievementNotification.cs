using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementNotification : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI achievementNameText;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image iconImage;

    [Header("Animation")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float peakScale = 1.2f;
    [SerializeField] private float scaleUpTime = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioClip unlockSound;

    private Queue<AchievementInfo> pendingNotifications = new Queue<AchievementInfo>();
    private bool isAnimating = false;
    private float animTimer = 0f;
    private Vector3 baseScale;
    private AudioSource audioSource;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (notificationPanel != null)
        {
            baseScale = notificationPanel.transform.localScale;
            canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
            }
            notificationPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnAchievementUnlocked.AddListener(OnAchievementUnlocked);
        }
    }

    void OnDestroy()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnAchievementUnlocked.RemoveListener(OnAchievementUnlocked);
        }
    }

    void OnAchievementUnlocked(AchievementInfo info)
    {
        pendingNotifications.Enqueue(info);
        if (!isAnimating) ShowNext();
    }

    void ShowNext()
    {
        if (pendingNotifications.Count == 0)
        {
            isAnimating = false;
            return;
        }

        AchievementInfo info = pendingNotifications.Dequeue();

        if (achievementNameText != null)
            achievementNameText.text = info.displayName;

        if (labelText != null)
            labelText.text = "Achievement Unlocked!";

        if (notificationPanel != null)
        {
            notificationPanel.transform.localScale = baseScale * startScale;
            canvasGroup.alpha = 0f;
            notificationPanel.SetActive(true);
        }

        if (unlockSound != null && audioSource != null)
            audioSource.PlayOneShot(unlockSound);

        isAnimating = true;
        animTimer = 0f;
    }

    void Update()
    {
        if (!isAnimating || notificationPanel == null) return;

        animTimer += Time.deltaTime;

        if (animTimer < scaleUpTime)
        {
            // Scale up and fade in
            float t = animTimer / scaleUpTime;
            float scale = Mathf.Lerp(startScale, peakScale, t);
            notificationPanel.transform.localScale = baseScale * scale;
            canvasGroup.alpha = t;
        }
        else if (animTimer < displayDuration)
        {
            // Hold and settle scale, fade out in last 40%
            float t = (animTimer - scaleUpTime) / (displayDuration - scaleUpTime);
            float scale = Mathf.Lerp(peakScale, 1f, t * 0.5f);
            notificationPanel.transform.localScale = baseScale * scale;

            float fadeStart = 0.6f;
            if (t > fadeStart)
            {
                canvasGroup.alpha = 1f - ((t - fadeStart) / (1f - fadeStart));
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }
        else
        {
            // Done - hide and show next
            notificationPanel.SetActive(false);
            isAnimating = false;
            ShowNext();
        }
    }
}
