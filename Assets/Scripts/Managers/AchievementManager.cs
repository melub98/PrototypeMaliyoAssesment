using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class AchievementInfo
{
    public string id;
    public string displayName;
    public string description;
    public bool unlocked;
    public int progress;
    public int goal;
}

[System.Serializable]
public class AchievementSaveData
{
    public AchievementInfo[] achievements;
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    public UnityEvent<AchievementInfo> OnAchievementUnlocked = new UnityEvent<AchievementInfo>();

    private const string SAVE_KEY = "FlappyJumpAchievements";

    private AchievementSaveData saveData;

    // Per-run tracking (reset each game)
    private int hoopsThisRun;
    private int consecutiveCleanPasses;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SubscribeEvents();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // After scene reload, GameManager is a new instance.
        // Re-subscribe to the new GameManager's events.
        SubscribeEvents();
    }

    void SubscribeEvents()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnGameStart.AddListener(OnGameStart);
        GameManager.Instance.OnGameOver.AddListener(OnGameOver);
        GameManager.Instance.OnHoopPassed.AddListener(OnHoopPassed);
        GameManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
    }

    void UnsubscribeEvents()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnGameStart.RemoveListener(OnGameStart);
        GameManager.Instance.OnGameOver.RemoveListener(OnGameOver);
        GameManager.Instance.OnHoopPassed.RemoveListener(OnHoopPassed);
        GameManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
    }

    void OnGameStart()
    {
        hoopsThisRun = 0;
        consecutiveCleanPasses = 0;
    }

    void OnHoopPassed(bool wasClean)
    {
        hoopsThisRun++;

        if (wasClean)
        {
            consecutiveCleanPasses++;

            // "Clean Sweep" - 1 clean pass
            TryUnlock("clean_sweep");

            // "Untouchable" - 5 consecutive clean passes
            if (consecutiveCleanPasses >= 5)
            {
                TryUnlock("untouchable");
            }
        }
        else
        {
            consecutiveCleanPasses = 0;
        }

        // "Perfect Ten" - 10 hoops in a single run
        if (hoopsThisRun >= 10)
        {
            TryUnlock("perfect_ten");
        }

        // "Hoop is Life" - 100 cumulative hoops on medium/hard
        int difficulty = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GetDifficulty()
            : 1;

        if (difficulty >= 1)
        {
            AchievementInfo hoopLife = GetAchievement("hoop_is_life");
            if (hoopLife != null && !hoopLife.unlocked)
            {
                hoopLife.progress++;
                if (hoopLife.progress >= hoopLife.goal)
                {
                    TryUnlock("hoop_is_life");
                }
            }
        }
    }

    void OnScoreChanged(int score)
    {
        // "Century Club" - score 100+
        if (score >= 100)
        {
            TryUnlock("century_club");
        }
    }

    void OnGameOver()
    {
        SaveData();
    }

    void TryUnlock(string id)
    {
        AchievementInfo info = GetAchievement(id);
        if (info == null || info.unlocked) return;

        info.unlocked = true;
        SaveData();
        OnAchievementUnlocked.Invoke(info);
        Debug.Log($"Achievement Unlocked: {info.displayName}");
    }

    void InitializeDefaults()
    {
        saveData = new AchievementSaveData
        {
            achievements = new AchievementInfo[]
            {
                new AchievementInfo
                {
                    id = "clean_sweep",
                    displayName = "Clean Sweep",
                    description = "Pass through a hoop without touching edges",
                    unlocked = false, progress = 0, goal = 0
                },
                new AchievementInfo
                {
                    id = "perfect_ten",
                    displayName = "Perfect Ten",
                    description = "Pass through 10 hoops in a single run",
                    unlocked = false, progress = 0, goal = 0
                },
                new AchievementInfo
                {
                    id = "untouchable",
                    displayName = "Untouchable",
                    description = "5 clean passes in a row without touching edges",
                    unlocked = false, progress = 0, goal = 0
                },
                new AchievementInfo
                {
                    id = "century_club",
                    displayName = "Century Club",
                    description = "Score 100 points in a single run",
                    unlocked = false, progress = 0, goal = 0
                },
                new AchievementInfo
                {
                    id = "hoop_is_life",
                    displayName = "Hoop is Life",
                    description = "Pass through 100 hoops on Medium or Hard difficulty",
                    unlocked = false, progress = 0, goal = 100
                }
            }
        };
    }

    void SaveData()
    {
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    void LoadData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            saveData = JsonUtility.FromJson<AchievementSaveData>(json);

            // Ensure all 5 achievements exist (handles saves from older versions)
            if (saveData == null || saveData.achievements == null || saveData.achievements.Length < 5)
            {
                // Preserve any existing unlock data
                AchievementSaveData oldData = saveData;
                InitializeDefaults();

                if (oldData?.achievements != null)
                {
                    foreach (var old in oldData.achievements)
                    {
                        AchievementInfo current = GetAchievement(old.id);
                        if (current != null)
                        {
                            current.unlocked = old.unlocked;
                            current.progress = old.progress;
                        }
                    }
                }
            }
        }
        else
        {
            InitializeDefaults();
        }
    }

    public AchievementInfo GetAchievement(string id)
    {
        if (saveData?.achievements == null) return null;

        foreach (var a in saveData.achievements)
        {
            if (a.id == id) return a;
        }
        return null;
    }

    public AchievementInfo[] GetAllAchievements()
    {
        return saveData?.achievements;
    }

    public int GetUnlockedCount()
    {
        if (saveData?.achievements == null) return 0;

        int count = 0;
        foreach (var a in saveData.achievements)
        {
            if (a.unlocked) count++;
        }
        return count;
    }

    public int GetTotalCount()
    {
        return saveData?.achievements?.Length ?? 0;
    }

    /// <summary>
    /// Resets all achievements. Call from console or debug UI for testing.
    /// In editor: call via Inspector context menu on AchievementManager.
    /// </summary>
    [ContextMenu("Reset All Achievements")]
    public void ResetAllAchievements()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        InitializeDefaults();
        Debug.Log("AchievementManager: All achievements reset!");
    }
}
