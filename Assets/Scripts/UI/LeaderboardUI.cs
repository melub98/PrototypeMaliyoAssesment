using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the leaderboard UI display and player name entry.
/// Shows top scores with special coloring for top 3 positions.
/// Prompts player for name entry when achieving a high score.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI References")]
    [Tooltip("Main panel containing the leaderboard display")]
    [SerializeField] private GameObject leaderboardPanel;

    [Tooltip("Parent transform where leaderboard entry UI elements are spawned")]
    [SerializeField] private Transform entryContainer;

    [Tooltip("Prefab for individual leaderboard entry rows")]
    [SerializeField] private GameObject entryPrefab;

    [Tooltip("Input field for player to enter their name")]
    [SerializeField] private TMP_InputField nameInputField;

    [Tooltip("Panel shown when player achieves a high score to enter their name")]
    [SerializeField] private GameObject nameEntryPanel;

    [Tooltip("Button to submit score")]
    [SerializeField] private Button submitButton;

    [Tooltip("Button to close leaderboard")]
    [SerializeField] private Button closeButton;

    [Header("Colors")]
    [Tooltip("Color for 1st place entry (gold)")]
    [SerializeField] private Color firstPlaceColor = new Color(1f, 0.84f, 0f); // Gold

    [Tooltip("Color for 2nd place entry (silver)")]
    [SerializeField] private Color secondPlaceColor = new Color(0.75f, 0.75f, 0.75f); // Silver

    [Tooltip("Color for 3rd place entry (bronze)")]
    [SerializeField] private Color thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f); // Bronze

    [Tooltip("Color for other entries")]
    [SerializeField] private Color normalColor = Color.white;

    [Header("Audio")]
    [Tooltip("Sound played when new high score is achieved")]
    [SerializeField] private AudioClip highScoreSound;

    [Tooltip("Sound played when score is submitted")]
    [SerializeField] private AudioClip submitSound;

    #endregion

    #region Private Fields

    // Stores the player's score when game ends for submission
    private int currentScore;
    // Audio source for sounds
    private AudioSource audioSource;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Subscribe to game over event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(CheckForHighScore);
        }

        // Setup button listeners
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitScore);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideLeaderboard);
        }

        // Initialize UI state
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (nameEntryPanel != null) nameEntryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(CheckForHighScore);
        }
    }

    #endregion

    #region High Score Check

    /// <summary>
    /// Called when game ends to check if player achieved a high score.
    /// Shows name entry panel if score qualifies for leaderboard.
    /// </summary>
    void CheckForHighScore()
    {
        if (GameManager.Instance == null) return;
        if (LeaderboardManager.Instance == null) return;

        // Get the final score from GameManager
        currentScore = GameManager.Instance.GetScore();

        // Show name entry if this score qualifies for the leaderboard
        if (LeaderboardManager.Instance.IsHighScore(currentScore))
        {
            ShowNameEntryPanel();
        }
    }

    /// <summary>
    /// Shows the name entry panel for high score submission.
    /// </summary>
    void ShowNameEntryPanel()
    {
        if (nameEntryPanel != null)
        {
            nameEntryPanel.SetActive(true);

            // Clear previous input
            if (nameInputField != null)
            {
                nameInputField.text = "";
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }

            // Play high score sound
            PlaySound(highScoreSound);
        }
    }

    #endregion

    #region Score Submission

    /// <summary>
    /// Button callback for submit button on name entry panel.
    /// Saves the score with entered name and shows leaderboard.
    /// </summary>
    public void SubmitScore()
    {
        if (LeaderboardManager.Instance == null) return;

        // Get player name, default to "Anonymous" if empty
        string playerName = nameInputField != null ? nameInputField.text : "";
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Anonymous";
        }

        // Sanitize name (limit length, remove special characters)
        playerName = SanitizeName(playerName);

        // Add score to leaderboard
        LeaderboardManager.Instance.AddScore(playerName, currentScore);

        // Play submit sound
        PlaySound(submitSound);

        // Hide name entry and show leaderboard
        if (nameEntryPanel != null) nameEntryPanel.SetActive(false);
        ShowLeaderboard();
    }

    /// <summary>
    /// Sanitizes player name input.
    /// </summary>
    string SanitizeName(string name)
    {
        // Trim whitespace
        name = name.Trim();

        // Limit length
        if (name.Length > 15)
        {
            name = name.Substring(0, 15);
        }

        return name;
    }

    #endregion

    #region Leaderboard Display

    /// <summary>
    /// Shows the leaderboard panel and populates it with current scores.
    /// </summary>
    public void ShowLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            PopulateLeaderboard();
        }
    }

    /// <summary>
    /// Hides the leaderboard panel.
    /// </summary>
    public void HideLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Toggles leaderboard visibility.
    /// </summary>
    public void ToggleLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            if (leaderboardPanel.activeSelf)
            {
                HideLeaderboard();
            }
            else
            {
                ShowLeaderboard();
            }
        }
    }

    /// <summary>
    /// Populates the leaderboard display with current top scores.
    /// Creates UI entries for each score with appropriate styling.
    /// </summary>
    void PopulateLeaderboard()
    {
        if (entryContainer == null || entryPrefab == null) return;
        if (LeaderboardManager.Instance == null) return;

        // Clear any existing entry UI elements
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }

        // Get top scores from LeaderboardManager
        List<LeaderboardEntry> entries = LeaderboardManager.Instance.GetTopScores();

        // Create UI entry for each score
        for (int i = 0; i < entries.Count; i++)
        {
            CreateLeaderboardEntry(i, entries[i]);
        }
    }

    /// <summary>
    /// Creates a single leaderboard entry UI element.
    /// </summary>
    void CreateLeaderboardEntry(int index, LeaderboardEntry entry)
    {
        // Instantiate entry prefab as child of container
        GameObject entryObj = Instantiate(entryPrefab, entryContainer);
        entryObj.name = $"Entry_{index + 1}";

        // Get text components from prefab
        TextMeshProUGUI[] texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

        // Populate text fields (expects 4: rank, name, score, date)
        if (texts.Length >= 1) texts[0].text = (index + 1).ToString();     // Rank
        if (texts.Length >= 2) texts[1].text = entry.playerName;            // Name
        if (texts.Length >= 3) texts[2].text = entry.score.ToString();      // Score
        if (texts.Length >= 4) texts[3].text = entry.date;                  // Date

        // Apply color based on rank
        Color entryColor = GetRankColor(index);
        SetEntryColor(texts, entryColor);

        // Highlight if this is the player's current score
        if (entry.score == currentScore)
        {
            HighlightEntry(entryObj);
        }
    }

    /// <summary>
    /// Gets the appropriate color for a rank position.
    /// </summary>
    Color GetRankColor(int index)
    {
        switch (index)
        {
            case 0: return firstPlaceColor;  // Gold
            case 1: return secondPlaceColor; // Silver
            case 2: return thirdPlaceColor;  // Bronze
            default: return normalColor;
        }
    }

    /// <summary>
    /// Sets the color of all text elements in a leaderboard entry.
    /// </summary>
    void SetEntryColor(TextMeshProUGUI[] texts, Color color)
    {
        foreach (var text in texts)
        {
            if (text != null)
            {
                text.color = color;
            }
        }
    }

    /// <summary>
    /// Highlights a leaderboard entry (for current player's score).
    /// </summary>
    void HighlightEntry(GameObject entryObj)
    {
        // Add a background highlight or scale effect
        Image bg = entryObj.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(1f, 1f, 1f, 0.2f);
        }
    }

    #endregion

    #region Audio

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion
}
