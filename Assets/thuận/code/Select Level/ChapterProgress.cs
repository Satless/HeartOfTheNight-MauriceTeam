using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tiến độ Chapter 1 (Floor 1 Khánh). Qua scene nào thì unlock nút scene đó trên Select Level.
/// </summary>
public static class ChapterProgress
{
    public const string PrefsKey = "Chapter1.UnlockedScenes";

    public static readonly string[] Chapter1Scenes =
    {
        "Khanh_Level0-1",
        "Khanh_Level1-1",
        "Khanh_Level2-1",
        "Khanh_Level3-1",
        "Khanh_Level4-1",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        UnlockIfChapterScene(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnlockIfChapterScene(scene.name);
    }

    public static int IndexOf(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return -1;

        for (var i = 0; i < Chapter1Scenes.Length; i++)
        {
            if (string.Equals(Chapter1Scenes[i], sceneName, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public static bool IsUnlocked(string sceneName)
    {
        var index = IndexOf(sceneName);
        if (index <= 0)
            return true;

        return GetUnlocked().Contains(Chapter1Scenes[index]);
    }

    public static void UnlockScene(string sceneName)
    {
        var index = IndexOf(sceneName);
        if (index < 0)
            return;

        var unlocked = GetUnlocked();
        var canonical = Chapter1Scenes[index];
        if (!unlocked.Add(canonical))
            return;

        Save(unlocked);

        var dm = HeartOfTheNight.Hung.DataManager.Instance;
        if (dm?.Data != null && index + 1 > dm.Data.maxUnlockedLevel)
            dm.Data.maxUnlockedLevel = index + 1;
    }

    public static void UnlockIfChapterScene(string sceneName)
    {
        if (IndexOf(sceneName) >= 0)
            UnlockScene(sceneName);
    }

    private static HashSet<string> GetUnlocked()
    {
        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { Chapter1Scenes[0] };
        var raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
            return set;

        var parts = raw.Split('|');
        for (var i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i]))
                set.Add(parts[i].Trim());
        }

        return set;
    }

    private static void Save(HashSet<string> unlocked)
    {
        PlayerPrefs.SetString(PrefsKey, string.Join("|", unlocked));
        PlayerPrefs.Save();
    }
}
