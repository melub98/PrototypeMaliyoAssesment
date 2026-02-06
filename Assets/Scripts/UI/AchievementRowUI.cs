using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Colors")]
    [SerializeField] private Color unlockedNameColor = new Color(1f, 0.85f, 0f); // Gold
    [SerializeField] private Color lockedNameColor = Color.gray;

    public void Setup(AchievementInfo info)
    {
        if (info == null) return;

        if (nameText != null)
            nameText.text = info.displayName;

        if (descriptionText != null)
            descriptionText.text = info.description;

        if (info.unlocked)
        {
            if (iconImage != null && unlockedSprite != null)
                iconImage.sprite = unlockedSprite;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            if (nameText != null)
                nameText.color = unlockedNameColor;
        }
        else
        {
            if (iconImage != null && lockedSprite != null)
                iconImage.sprite = lockedSprite;
            if (canvasGroup != null)
                canvasGroup.alpha = 0.5f;
            if (nameText != null)
                nameText.color = lockedNameColor;
        }

        // Show progress only for cumulative achievements that aren't unlocked
        if (progressText != null)
        {
            if (info.goal > 0 && !info.unlocked)
            {
                progressText.gameObject.SetActive(true);
                progressText.text = $"{info.progress}/{info.goal}";
            }
            else
            {
                progressText.gameObject.SetActive(false);
            }
        }
    }
}
