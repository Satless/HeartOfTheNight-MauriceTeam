using System;
using System.Collections.Generic;
using System.IO;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    public partial class DataManager
    {
        public void SaveGame()
        {
            if (Data == null) Data = new GameData();
            Data.hasSave = true;
            Data.slotIndex = ActiveSlotIndex;
            Data.lastPlayedAtUtc = DateTime.UtcNow.ToString("o");
            _playTimeDirty = false;
            _playTimeSaveTimer = 0f;

            SaveGameLocal();

            if (_isFirebaseReady && _user != null && _dbRef != null)
            {
                string json = JsonUtility.ToJson(Data, true);
                GetSlotDbRef().SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        Debug.LogError("[Firebase] Lỗi khi lưu lên Cloud.");
                    else
                        Debug.Log($"[Firebase] Đã đồng bộ Slot {ActiveSlotIndex} lên Cloud thành công!");
                });
            }
        }

        public void LoadGame(Action onLoaded = null)
        {
            if (_isFirebaseReady && _user != null && _dbRef != null)
            {
                LoadGameCloud(onLoaded);
            }
            else if (_isFirebaseInitializing)
            {
                Debug.LogWarning("[Firebase] Hệ thống mạng đang khởi tạo, xin vui lòng đợi...");
                StartCoroutine(WaitAndLoadCloud(onLoaded));
            }
            else
            {
                LoadGameLocal();
                onLoaded?.Invoke();
            }
        }

        internal void DeleteCloudSlot(int slotIndex)
        {
            if (!_isFirebaseReady || _user == null || _dbRef == null)
                return;

            _dbRef.Child("users").Child(_user.UserId).Child("slots").Child(slotIndex.ToString())
                .RemoveValueAsync();
        }

        private DatabaseReference GetSlotDbRef()
        {
            return _dbRef.Child("users").Child(_user.UserId).Child("slots").Child(ActiveSlotIndex.ToString()).Child("GameData");
        }

        private void KeepBetterLocalPlayTime()
        {
            if (Data == null)
                return;

            if (!SaveSlotStorage.TryReadSlotFromDisk(ActiveSlotIndex, out GameData local) || local == null)
                return;

            if (local.totalPlayTimeSeconds > Data.totalPlayTimeSeconds)
                Data.totalPlayTimeSeconds = local.totalPlayTimeSeconds;
        }

        private System.Collections.IEnumerator WaitAndLoadCloud(Action onLoaded)
        {
            while (_isFirebaseInitializing)
                yield return null;

            LoadGame(onLoaded);
        }

        private void LoadGameCloud(Action onLoaded = null)
        {
            Debug.Log($"[Firebase] Đang tải Slot {ActiveSlotIndex} từ Cloud...");
            GetSlotDbRef().GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[Firebase] Lỗi tải Cloud Data. Fallback sang Local Load.");
                    LoadGameLocal();
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot != null && snapshot.Exists)
                    {
                        string json = snapshot.GetRawJsonValue();

                        if (Data == null) Data = new GameData();
                        JsonUtility.FromJsonOverwrite(json, Data);
                        KeepBetterLocalPlayTime();

                        Debug.Log($"[Firebase] Tải Cloud thành công. Kiểm tra RAM: playerHealth={Data.playerHealth}");

                        SaveGameLocal();
                        ApplyEditorKeyResetIfNeeded();
                        HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                    }
                    else if (ActiveSlotIndex == 1)
                    {
                        TryLoadLegacyCloudThenLocal(onLoaded);
                        return;
                    }
                    else
                    {
                        Debug.Log($"[Firebase] Slot {ActiveSlotIndex} chưa có trên Cloud. Đang tải Local...");
                        LoadGameLocal();
                    }
                }

                onLoaded?.Invoke();
                ApplyEditorKeyResetIfNeeded();
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
            });
        }

        private void TryLoadLegacyCloudThenLocal(Action onLoaded)
        {
            _dbRef.Child("users").Child(_user.UserId).Child("GameData").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result != null && task.Result.Exists)
                {
                    if (Data == null) Data = new GameData();
                    JsonUtility.FromJsonOverwrite(task.Result.GetRawJsonValue(), Data);
                    Data.slotIndex = 1;
                    Data.hasSave = true;
                    KeepBetterLocalPlayTime();
                    SaveGameLocal();
                    Debug.Log("[Firebase] Đã migrate GameData cũ → Slot 1.");
                }
                else
                {
                    LoadGameLocal();
                }

                onLoaded?.Invoke();
                ApplyEditorKeyResetIfNeeded();
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
            });
        }

        private void SaveGameLocal()
        {
            try
            {
                SaveSlotStorage.WriteSlotAtomic(Data, ActiveSlotIndex);
                Debug.Log("[Save System] Đã lưu local thành công.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save System] Lỗi khi lưu local: {e.Message}");
            }
        }

        private void LoadGameLocal()
        {
            if (Data == null) Data = new GameData();

            try
            {
                if (SaveSlotStorage.TryLoadSlotFile(ActiveSlotIndex, Data))
                {
                    Data.hasSave = true;
                    Data.slotIndex = ActiveSlotIndex;
                    Debug.Log($"[Save System] Tải local Slot {ActiveSlotIndex} thành công. RAM: playerHealth={Data.playerHealth}");
                    ApplyEditorKeyResetIfNeeded();
                    HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save System] File chính bị hỏng, tải từ Backup. Lỗi: {e.Message}");
                LoadFromBackupLocal();
                return;
            }

            if (ActiveSlotIndex == 1)
            {
                try
                {
                    if (SaveSlotStorage.TryMigrateLegacySaveData(Data))
                    {
                        Data.hasSave = true;
                        Data.slotIndex = 1;
                        SaveGameLocal();
                        Debug.Log("[Save System] Đã migrate save_data.json → save_slot_1.json");
                        ApplyEditorKeyResetIfNeeded();
                        HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Save System] Migrate legacy thất bại: {e.Message}");
                }
            }

            LoadFromBackupLocal();
        }

        private void LoadFromBackupLocal()
        {
            if (Data == null) Data = new GameData();

            try
            {
                if (SaveSlotStorage.TryLoadBackupFile(ActiveSlotIndex, Data))
                {
                    Debug.Log("[Save System] Đã khôi phục thành công từ file Backup.");
                    ApplyEditorKeyResetIfNeeded();
                    HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save System] File Backup cũng bị lỗi! Tạo data mới hoàn toàn. Lỗi: {e.Message}");
            }

            Debug.Log("[Save System] Chưa có file save nào (Game mới). Bắt đầu với Data gốc.");
            Data = new GameData();
            ApplyEditorKeyResetIfNeeded();
            HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
        }

        private void ApplyEditorKeyResetIfNeeded()
        {
#if UNITY_EDITOR
            if (keepSavedKeysWhenPlayInEditor || Data == null) return;

            Data.blueKeys = 0;
            Data.redKeys = 0;
            Data.collectedBlueKey = false;
            Data.collectedRedKey = false;
            if (Data.collectedKeyPickupIds == null)
                Data.collectedKeyPickupIds = new List<string>();
            else
                Data.collectedKeyPickupIds.Clear();
            Debug.Log("[DataManager] Editor: reset chìa về 0 cho session Play này.");
#endif
        }
    }
}
