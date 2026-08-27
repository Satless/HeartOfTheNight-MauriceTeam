using System;
using System.IO;
using Firebase.Auth;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    /// <summary>Đường dẫn + đọc/xóa file save local (theo guest / Google UID).</summary>
    public static class SaveSlotStorage
    {
        public const string ActiveSlotPrefsKey = "Save.ActiveSlot";

        public static string GetSlotSavePath(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);
            return Path.Combine(GetAccountSaveFolder(create: false), $"save_slot_{slotIndex}.json");
        }

        public static string GetSlotBackupPath(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);
            return Path.Combine(GetAccountSaveFolder(create: false), $"save_slot_{slotIndex}.bak");
        }

        public static string GetSlotTempPath(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);
            return Path.Combine(GetAccountSaveFolder(create: false), $"save_slot_{slotIndex}.tmp");
        }

        public static string GetAccountSaveFolder(bool create)
        {
            string key = GetAccountSaveKey();
            if (key == "guest")
                return Application.persistentDataPath;

            string folder = Path.Combine(Application.persistentDataPath, "saves", key);
            if (create && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        public static string GetAccountSaveKey()
        {
            FirebaseUser user = DataManager.Instance != null ? DataManager.Instance.FirebaseUser : null;
            if (user == null)
            {
                try
                {
                    if (FirebaseAuth.DefaultInstance != null)
                        user = FirebaseAuth.DefaultInstance.CurrentUser;
                }
                catch
                {
                    // Firebase chưa sẵn sàng
                }
            }

            if (user == null || user.IsAnonymous)
                return "guest";
            return user.UserId;
        }

        public static bool UsesSharedGuestFolder()
        {
            return GetAccountSaveKey() == "guest";
        }

        public static bool HasSave(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);
            if (File.Exists(GetSlotSavePath(slotIndex)))
                return true;

            if (UsesSharedGuestFolder()
                && slotIndex == 1
                && File.Exists(Path.Combine(Application.persistentDataPath, "save_data.json")))
                return true;

            return false;
        }

        public static int GetActiveSlotIndex()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotPrefsKey, 1), 1, DataManager.SlotCount);
        }

        public static void SetActiveSlotIndex(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);
            PlayerPrefs.SetInt(ActiveSlotPrefsKey, slotIndex);
            PlayerPrefs.Save();
        }

        public static bool TryPeekSlot(int slotIndex, out GameData peek)
        {
            peek = null;
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);

            TryReadSlotFromDisk(slotIndex, out GameData disk);
            float diskPlayTime = disk != null ? disk.totalPlayTimeSeconds : 0f;

            var instance = DataManager.Instance;
            if (instance != null
                && instance.ActiveSlotIndex == slotIndex
                && instance.Data != null
                && instance.Data.hasSave)
            {
                if (diskPlayTime > instance.Data.totalPlayTimeSeconds)
                    instance.Data.totalPlayTimeSeconds = diskPlayTime;
                peek = instance.Data;
                return true;
            }

            peek = disk;
            return peek != null;
        }

        public static bool TryReadSlotFromDisk(int slotIndex, out GameData peek)
        {
            peek = null;
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);

            TryLoadGameDataFile(GetSlotSavePath(slotIndex), ref peek);
            TryLoadGameDataFile(GetSlotBackupPath(slotIndex), ref peek);

            if (UsesSharedGuestFolder() && slotIndex == 1)
            {
                string legacy = Path.Combine(Application.persistentDataPath, "save_data.json");
                TryLoadGameDataFile(legacy, ref peek);
            }

            return peek != null;
        }

        public static void DeleteLocalSlot(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, DataManager.SlotCount);
            TryDeleteFile(GetSlotSavePath(slotIndex));
            TryDeleteFile(GetSlotBackupPath(slotIndex));
            TryDeleteFile(GetSlotTempPath(slotIndex));

            if (UsesSharedGuestFolder() && slotIndex == 1)
            {
                TryDeleteFile(Path.Combine(Application.persistentDataPath, "save_data.json"));
                TryDeleteFile(Path.Combine(Application.persistentDataPath, "save_data.bak"));
                TryDeleteFile(Path.Combine(Application.persistentDataPath, "save_data.tmp"));
            }
        }

        public static void WriteSlotAtomic(GameData data, int slotIndex)
        {
            GetAccountSaveFolder(create: true);
            string savePath = GetSlotSavePath(slotIndex);
            string backupPath = GetSlotBackupPath(slotIndex);
            string tempPath = GetSlotTempPath(slotIndex);

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(tempPath, json);
            if (File.Exists(savePath))
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Move(savePath, backupPath);
            }

            File.Move(tempPath, savePath);
        }

        public static bool TryLoadSlotFile(int slotIndex, GameData target)
        {
            string savePath = GetSlotSavePath(slotIndex);
            if (!File.Exists(savePath))
                return false;

            JsonUtility.FromJsonOverwrite(File.ReadAllText(savePath), target);
            return true;
        }

        public static bool TryLoadBackupFile(int slotIndex, GameData target)
        {
            string backupPath = GetSlotBackupPath(slotIndex);
            if (!File.Exists(backupPath))
                return false;

            JsonUtility.FromJsonOverwrite(File.ReadAllText(backupPath), target);
            return true;
        }

        public static bool TryMigrateLegacySaveData(GameData target)
        {
            if (!UsesSharedGuestFolder())
                return false;

            string legacy = Path.Combine(Application.persistentDataPath, "save_data.json");
            if (!File.Exists(legacy))
                return false;

            JsonUtility.FromJsonOverwrite(File.ReadAllText(legacy), target);
            return true;
        }

        public static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save System] Không xóa được {path}: {e.Message}");
            }
        }

        private static void TryLoadGameDataFile(string path, ref GameData peek)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            try
            {
                GameData loaded = JsonUtility.FromJson<GameData>(File.ReadAllText(path));
                if (loaded == null)
                    return;

                if (peek == null)
                {
                    peek = loaded;
                    return;
                }

                if (loaded.totalPlayTimeSeconds > peek.totalPlayTimeSeconds)
                    peek.totalPlayTimeSeconds = loaded.totalPlayTimeSeconds;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save System] Không đọc được {path}: {e.Message}");
            }
        }
    }
}
