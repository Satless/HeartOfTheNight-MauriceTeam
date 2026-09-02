using UnityEngine.SceneManagement;

/// <summary>
/// Story1/2/3 và EndScene không nằm trên Select Level.
/// Story1 chạy trước 0-1 (New Game / chọn màn).
/// Hết 5-1 / 5-2 / 2-3 vẫn hiện Level Complete, Continue mới ra Story2 / Story3 / EndScene.
/// </summary>
public static class StoryFlow
{
    public const string Story1 = "Story1";
    public const string Story2 = "Story2";
    public const string Story3 = "Story3";
    public const string EndScene = "EndScene";

    private static string _pendingSpawnAfterStory = "";

    public static bool IsCinematic(string sceneName)
    {
        return NamesEqual(sceneName, Story1)
            || NamesEqual(sceneName, Story2)
            || NamesEqual(sceneName, Story3)
            || NamesEqual(sceneName, EndScene);
    }

    /// <summary>Select Level / New Game vào màn đầu chapter → scene intro (nếu có).</summary>
    public static string IntroForEnteringLevel(string levelScene)
    {
        if (IsFirstOf(ChapterProgress.Chapter1Scenes, levelScene))
            return Story1;
        if (IsFirstOf(ChapterProgress.Chapter2Scenes, levelScene))
            return Story2;
        if (IsFirstOf(ChapterProgress.Chapter3Scenes, levelScene))
            return Story3;
        return "";
    }

    /// <summary>Hết màn gameplay: load story/ending thay vì màn kế (Level Complete vẫn hiện trước).</summary>
    public static string CinematicAfterCompletingLevel(string completedLevel)
    {
        if (IsLastOf(ChapterProgress.Chapter1Scenes, completedLevel))
            return Story2;
        if (IsLastOf(ChapterProgress.Chapter2Scenes, completedLevel))
            return Story3;
        if (IsLastOf(ChapterProgress.Chapter3Scenes, completedLevel))
            return EndScene;
        return "";
    }

    public static string LevelAfterCinematic(string cinematicScene)
    {
        if (NamesEqual(cinematicScene, Story1))
            return ChapterProgress.Chapter1Scenes[0];
        if (NamesEqual(cinematicScene, Story2))
            return ChapterProgress.Chapter2Scenes[0];
        if (NamesEqual(cinematicScene, Story3))
            return ChapterProgress.Chapter3Scenes[0];
        return "";
    }

    /// <summary>Scene thực sự load sau cửa Next Level / Level Complete Continue.</summary>
    public static string ResolveLoadAfterLevel(string completedLevel, string doorNextScene)
    {
        string cinematic = CinematicAfterCompletingLevel(completedLevel);
        return string.IsNullOrEmpty(cinematic) ? doorNextScene : cinematic;
    }

    /// <summary>
    /// Scene gameplay cần PrepareForNewScene khi đi tới destination.
    /// Story2 → 1-2. EndScene → rỗng (không phải màn chơi).
    /// Prepare thật sự chỉ lúc ContinueFromStory / Select Level, không lúc vừa hiện story.
    /// </summary>
    public static string GameplaySceneForDestination(string destination)
    {
        if (!IsCinematic(destination))
            return destination;

        return LevelAfterCinematic(destination);
    }

    public static void RememberSpawnForNextLevel(string spawnId)
    {
        _pendingSpawnAfterStory = spawnId ?? "";
    }

    public static string ConsumePendingSpawn()
    {
        string spawnId = _pendingSpawnAfterStory;
        _pendingSpawnAfterStory = "";
        return spawnId;
    }

    public static void ApplyDestinationSave(string loadScene, string spawnId, bool saveCheckpoint, int health)
    {
        var dm = HeartOfTheNight.Hung.DataManager.Instance;
        if (dm == null || dm.Data == null)
            return;

        string gameplay = GameplaySceneForDestination(loadScene);
        bool goingToStory = IsCinematic(loadScene) && !string.IsNullOrEmpty(gameplay);
        bool goingToEnding = NamesEqual(loadScene, EndScene);

        if (goingToStory)
        {
            // Chốt màn vừa thắng, chưa wipe màn kế — Back khỏi story không được xóa 1-2.
            // ContinueFromStory mới PrepareForNewScene khi thật sự vào gameplay.
            dm.Data.currentScene = gameplay;
            dm.CommitFinishedLevelAndLeave();
            RememberSpawnForNextLevel(spawnId);
            LevelEntrance.ClearPendingSpawn();
            return;
        }

        if (goingToEnding)
        {
            dm.Data.currentScene = loadScene;
            dm.CommitFinishedLevelAndLeave();
            RememberSpawnForNextLevel("");
            LevelEntrance.ClearPendingSpawn();
            return;
        }

        dm.Data.currentScene = loadScene;
        dm.PrepareForNewScene(loadScene);

        if (saveCheckpoint)
            dm.SaveCheckpoint(loadScene, spawnId, UnityEngine.Vector3.zero, health);
        else
            dm.ClearCheckpointAfterLeavingLevel();
    }

    public static void ContinueFromStory(string storyScene, string overrideNextLevel)
    {
        string next = overrideNextLevel;
        if (string.IsNullOrEmpty(next))
            next = LevelAfterCinematic(storyScene);

        if (string.IsNullOrEmpty(next))
        {
            UnityEngine.Debug.LogError($"[StoryFlow] Scene sau {storyScene} chưa được gán.");
            return;
        }

        ChapterProgress.UnlockIfChapterScene(next);

        var dm = HeartOfTheNight.Hung.DataManager.Instance;
        if (dm != null && dm.Data != null)
        {
            dm.Data.currentScene = next;
            // Điểm wipe màn đích: replay/speedrun khi thật sự vào gameplay sau story.
            dm.PrepareForNewScene(next);
            dm.ClearCheckpointAfterLeavingLevel();
        }

        LevelEntrance.SetPendingSpawn(ConsumePendingSpawn());
        LoadScene(next);
    }

    public static void LoadScene(string sceneName)
    {
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.LoadSceneWithLoading(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    private static bool IsFirstOf(string[] chapter, string sceneName)
    {
        return chapter != null && chapter.Length > 0 && NamesEqual(chapter[0], sceneName);
    }

    private static bool IsLastOf(string[] chapter, string sceneName)
    {
        return chapter != null && chapter.Length > 0
            && NamesEqual(chapter[chapter.Length - 1], sceneName);
    }

    private static bool NamesEqual(string a, string b)
    {
        return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
    }
}
