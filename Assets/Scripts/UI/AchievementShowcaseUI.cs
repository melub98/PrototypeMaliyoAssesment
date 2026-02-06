using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementShowcaseUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject achievementPanel;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI headerText;

    [Header("Achievement Rows (assign 5 in order)")]
    [SerializeField] private AchievementRowUI[] rows;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    void Awake()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    public void ShowPanel()
    {
        RefreshDisplay();
        if (achievementPanel != null)
            achievementPanel.SetActive(true);
    }

    public void HidePanel()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(false);
    }

    void RefreshDisplay()
    {
        if (AchievementManager.Instance == null) return;

        AchievementInfo[] achievements = AchievementManager.Instance.GetAllAchievements();
        if (achievements == null) return;

        // Update header
        if (headerText != null)
        {
            int unlocked = AchievementManager.Instance.GetUnlockedCount();
            int total = AchievementManager.Instance.GetTotalCount();
            headerText.text = $"Achievements ({unlocked}/{total})";
        }

        // Update rows
        if (rows != null)
        {
            for (int i = 0; i < rows.Length && i < achievements.Length; i++)
            {
                if (rows[i] != null)
                    rows[i].Setup(achievements[i]);
            }
        }
    }
}
