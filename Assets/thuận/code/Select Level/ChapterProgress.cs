using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unlock Select Level: Chapter 1 (6) → Chapter 2 (5) → Chapter 3 (2).
/// Scene nào đã vào thì mở nút scene đó. Scene đầu Chapter 1 luôn mở.
/// Qua cửa hết màn thì mở luôn scene kế trong chuỗi (kể cả chapter sau).
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
        "Khanh_Level5-1",
    };

    public static readonly string[] Chapter2Scenes =
    {
        "khanh_level1-2",
        "khanh_level2-2",
        "khanh_level3-2",
        "Khanh_level4-2",
        "Khanh_level5-2",
    };

    public static readonly string[] Chapter3Scenes =
    {
        "Khanh_Level1-3",
        "Khanh_Level2-3",
    };

    public static readonly string[][] Chapters =
    {
        Chapter1Scenes,
        Chapter2Scenes,
        Chapter3Scenes,
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

        int offset = 0;
        for (int c = 0; c < Chapters.Length; c++)
        {
            var list = Chapters[c];
            for (int i = 0; i < list.Length; i++)
            {
                if (string.Equals(list[i], sceneName, System.StringComparison.OrdinalIgnoreCase))
                    return offset + i;
            }

            offset += list.Length;
        }

        return -1;
    }

    public static int TotalSceneCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Chapters.Length; i++)
                n += Chapters[i].Length;
            return n;
        }
    }

    public static string GetSceneAt(int globalIndex)
    {
        return CanonicalName(globalIndex);
    }

    public static bool IsUnlocked(string sceneName)
    {
        var index = IndexOf(sceneName);
        if (index < 0)
            return false;
        if (index == 0)
            return true;

        return GetUnlocked().Contains(CanonicalName(index));
    }

    public static string DisplayName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return "";

        const string prefix = "Khanh_Level";
        if (sceneName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            return "Level " + sceneName.Substring(prefix.Length);
        return sceneName;
    }

    public static void UnlockScene(string sceneName)
    {
        var index = IndexOf(sceneName);
        if (index < 0)
            return;

        var unlocked = GetUnlocked();
        var canonical = CanonicalName(index);
        bool added = unlocked.Add(canonical);

        if (added)
            Save(unlocked);

        var dm = HeartOfTheNight.Hung.DataManager.Instance;
        if (dm?.Data != null && index + 1 > dm.Data.maxUnlockedLevel)
        {
            dm.Data.maxUnlockedLevel = index + 1;
            // Trong màn: YOU WIN sẽ CommitFinishedLevelAndLeave. Ghi disk ngay lúc này
            // chỉ persist snapshot checkpoint + maxUnlocked → crash overlay hỏi Continue màn cũ.
            if (!HeartOfTheNight.Hung.DataManager.IsLevelScene(SceneManager.GetActiveScene().name))
                dm.PersistUnlockProgress();
        }
    }

    public static void UnlockIfChapterScene(string sceneName)
    {
        if (IndexOf(sceneName) >= 0)
            UnlockScene(sceneName);
    }

    /// <summary>
    /// Hết màn: mở scene hiện tại và scene kế trong chuỗi 6+5+2.
    /// Không phụ thuộc cửa ghi next = SelectLevel.
    /// </summary>
    public static void UnlockOnLeavingLevel(string currentSceneName)
    {
        int index = IndexOf(currentSceneName);
        if (index < 0)
            return;

        UnlockScene(currentSceneName);
        if (index + 1 < TotalSceneCount)
            UnlockScene(GetSceneAt(index + 1));
    }

    public static void ResetForNewSave()
    {
        ResetForSlot(ActiveSlotIndex());
    }

    public static void ResetForSlot(int slotIndex)
    {
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.DeleteKey(PrefsKeyForSlot(slotIndex));
        PlayerPrefs.Save();
    }

    public static void ApplyFromSave(HeartOfTheNight.Hung.GameData data)
    {
        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            Chapter1Scenes[0]
        };
        if (data == null)
        {
            Save(set);
            return;
        }

        int total = TotalSceneCount;
        int max = Mathf.Clamp(data.maxUnlockedLevel, 1, total);
        int cursor = 0;
        for (int c = 0; c < Chapters.Length; c++)
        {
            var list = Chapters[c];
            for (int i = 0; i < list.Length && cursor < max; i++, cursor++)
                set.Add(list[i]);
        }

        AddSceneAndPriors(set, data.currentScene);
        AddSceneAndPriors(set, data.checkpointScene);
        Save(set);
    }

    private static string CanonicalName(int globalIndex)
    {
        if (globalIndex < 0)
            return Chapter1Scenes[0];

        int offset = 0;
        for (int c = 0; c < Chapters.Length; c++)
        {
            var list = Chapters[c];
            if (globalIndex < offset + list.Length)
                return list[globalIndex - offset];
            offset += list.Length;
        }

        return Chapter1Scenes[0];
    }

    private static void AddSceneAndPriors(HashSet<string> set, string sceneName)
    {
        int index = IndexOf(sceneName);
        if (index < 0)
            return;

        int cursor = 0;
        for (int c = 0; c < Chapters.Length; c++)
        {
            var list = Chapters[c];
            for (int i = 0; i < list.Length; i++, cursor++)
            {
                if (cursor > index)
                    return;
                set.Add(list[i]);
            }
        }
    }

    private static int ActiveSlotIndex()
    {
        var dm = HeartOfTheNight.Hung.DataManager.Instance;
        if (dm == null)
            return HeartOfTheNight.Hung.SaveSlotStorage.GetActiveSlotIndex();
        return dm.ActiveSlotIndex;
    }

    private static string PrefsKeyForSlot(int slotIndex)
    {
        return PrefsKey + "." + Mathf.Clamp(slotIndex, 1, HeartOfTheNight.Hung.DataManager.SlotCount);
    }

    private static HashSet<string> GetUnlocked()
    {
        var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { Chapter1Scenes[0] };
        var raw = PlayerPrefs.GetString(PrefsKeyForSlot(ActiveSlotIndex()), string.Empty);
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
        PlayerPrefs.SetString(PrefsKeyForSlot(ActiveSlotIndex()), string.Join("|", unlocked));
        PlayerPrefs.Save();
    }
}
