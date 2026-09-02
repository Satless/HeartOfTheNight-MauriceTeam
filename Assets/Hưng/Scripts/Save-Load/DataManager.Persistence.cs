using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Firebase.Database;
using Firebase.Extensions;
using HeartOfTheNight.UI;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    public partial class DataManager
    {
        private readonly bool[] _cloudSlotExists = new bool[SlotCount];
        private readonly GameData[] _cloudSlotPeeks = new GameData[SlotCount];
        private readonly List<Action> _cloudSlotIndexWaiters = new List<Action>();
        private bool _cloudSlotIndexReady;
        private bool _cloudSlotIndexLoading;
        private int _cloudSlotIndexSerial;
        private bool _lastCloudLoadFailed;

        public bool IsWaitingForCloudSlots
        {
            get
            {
                if (_cloudSlotIndexLoading)
                    return true;
                if (_isFirebaseInitializing)
                    return !AuthSession.IsGuest;
                return UsesGoogleCloudSaves() && !_cloudSlotIndexReady;
            }
        }

        /// <summary>
        /// Ghi file/cloud. Chỉ gọi lúc qua cửa checkpoint, Pause Home / thoát màn, tạo-xóa slot.
        /// Không gọi khi nhặt chìa, clear phòng, hay mỗi phút trong màn.
        /// </summary>
        public void SaveGame()
        {
            if (Data == null) Data = new GameData();
            Data.EnsureLists();
            Data.hasSave = true;
            Data.slotIndex = ActiveSlotIndex;
            Data.lastPlayedAtUtc = DateTime.UtcNow.ToString("o");
            _playTimeDirty = false;
            _playTimeSaveTimer = 0f;

            SaveGameLocal();

            if (UsesGoogleCloudSaves())
            {
                string json = JsonUtility.ToJson(Data, true);
                GetSlotDbRef().SetRawJsonValueAsync(SaveCrypto.WrapForCloud(json)).ContinueWithOnMainThread(task =>
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
            if (ShouldPreserveLiveRamSave())
            {
                Debug.LogWarning("[Save System] Đang trong màn chơi — bỏ LoadGame để không đè chìa/phòng trên RAM.");
                onLoaded?.Invoke();
                return;
            }

            _lastCloudLoadFailed = false;
            if (UsesGoogleCloudSaves())
            {
                LoadGameCloud(onLoaded);
            }
            else if (_isFirebaseInitializing && !AuthSession.IsGuest)
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

        internal void DeleteCloudSlot(int slotIndex, Action<bool> onComplete)
        {
            if (!UsesGoogleCloudSaves())
            {
                onComplete?.Invoke(true);
                return;
            }

            DatabaseReference userRef = _dbRef.Child("users").Child(_user.UserId);
            int remaining = slotIndex == 1 ? 2 : 1;
            bool failed = false;

            void OnOneDone(System.Threading.Tasks.Task task)
            {
                if (task == null || task.IsFaulted || task.IsCanceled)
                    failed = true;

                remaining--;
                if (remaining > 0)
                    return;

                if (failed)
                    Debug.LogError($"[Firebase] Không xóa được Slot {slotIndex} trên Cloud.");
                onComplete?.Invoke(!failed);
            }

            userRef.Child("slots").Child(slotIndex.ToString()).RemoveValueAsync()
                .ContinueWithOnMainThread(OnOneDone);

            if (slotIndex == 1)
                userRef.Child("GameData").RemoveValueAsync().ContinueWithOnMainThread(OnOneDone);
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

        /// <summary>
        /// Cloud đè local sẽ mất tiến trình vừa chơi offline. Chọn bản lastPlayed mới hơn.
        /// </summary>
        private GameData PreferNewerSave(GameData cloud)
        {
            if (!SaveSlotStorage.TryReadSlotFromDisk(ActiveSlotIndex, out GameData local) || local == null || !local.hasSave)
                return cloud ?? new GameData();

            local.EnsureLists();
            if (cloud == null || !cloud.hasSave)
                return local;

            if (IsSaveNewer(local, cloud))
            {
                Debug.Log("[Save System] Local mới hơn Cloud — giữ bản máy, không đè tiến trình.");
                if (cloud.totalPlayTimeSeconds > local.totalPlayTimeSeconds)
                    local.totalPlayTimeSeconds = cloud.totalPlayTimeSeconds;
                return local;
            }

            return cloud;
        }

        private static bool IsSaveNewer(GameData a, GameData b)
        {
            if (a == null) return false;
            if (b == null) return true;

            if (TryParseUtc(a.lastPlayedAtUtc, out DateTime ta) && TryParseUtc(b.lastPlayedAtUtc, out DateTime tb))
                return ta > tb.AddSeconds(1);

            return a.totalPlayTimeSeconds > b.totalPlayTimeSeconds + 1f;
        }

        private static bool TryParseUtc(string iso, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(iso))
                return false;
            return DateTime.TryParse(iso, null, DateTimeStyles.RoundtripKind, out utc);
        }

        private System.Collections.IEnumerator WaitAndLoadCloud(Action onLoaded)
        {
            while (_isFirebaseInitializing)
                yield return null;

            LoadGame(onLoaded);
        }

        private void LoadGameCloud(Action onLoaded = null)
        {
            if (ShouldPreserveLiveRamSave())
            {
                Debug.LogWarning("[Firebase] Đang trong màn chơi — bỏ LoadGameCloud để không đè RAM.");
                onLoaded?.Invoke();
                return;
            }

            Debug.Log($"[Firebase] Đang tải Slot {ActiveSlotIndex} từ Cloud...");
            GetSlotDbRef().GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    _lastCloudLoadFailed = true;
                    Debug.LogError("[Firebase] Lỗi tải Cloud Data. Fallback sang Local Load.");
                    LoadGameLocal();
                }
                else if (task.IsCompleted)
                {
                    _lastCloudLoadFailed = false;
                    DataSnapshot snapshot = task.Result;
                    if (snapshot != null && snapshot.Exists)
                    {
                        string json = snapshot.GetRawJsonValue();

                        var cloud = new GameData();
                        try
                        {
                            SaveCrypto.OverwriteGameData(json, cloud);
                        }
                        catch (Exception e)
                        {
                            _lastCloudLoadFailed = true;
                            Debug.LogError("[Firebase] Cloud save không giải mã được. Fallback sang Local. " + e.Message);
                            LoadGameLocal();
                            onLoaded?.Invoke();
                            HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                            return;
                        }
                        cloud.hasSave = true;
                        cloud.slotIndex = ActiveSlotIndex;
                        cloud.EnsureLists();

                        Data = PreferNewerSave(cloud);
                        Data.hasSave = true;
                        Data.slotIndex = ActiveSlotIndex;
                        Data.EnsureLists();
                        KeepBetterLocalPlayTime();

                        Debug.Log($"[Firebase] Tải Cloud thành công. Kiểm tra RAM: playerHealth={Data.playerHealth}");

                        SaveGameLocal();
                        RememberCloudSlot(ActiveSlotIndex, Data);
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
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
            });
        }

        private void TryLoadLegacyCloudThenLocal(Action onLoaded)
        {
            _dbRef.Child("users").Child(_user.UserId).Child("GameData").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result != null && task.Result.Exists)
                {
                    _lastCloudLoadFailed = false;
                    Data = new GameData();
                    try
                    {
                        SaveCrypto.OverwriteGameData(task.Result.GetRawJsonValue(), Data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[Firebase] Legacy cloud save không giải mã được: " + e.Message);
                        LoadGameLocal();
                        onLoaded?.Invoke();
                        HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                        return;
                    }
                    Data.slotIndex = 1;
                    Data.hasSave = true;
                    Data.EnsureLists();
                    KeepBetterLocalPlayTime();
                    SaveGame();
                    if (UsesGoogleCloudSaves())
                        _dbRef.Child("users").Child(_user.UserId).Child("GameData").RemoveValueAsync();
                    RememberCloudSlot(1, Data);
                    Debug.Log("[Firebase] Đã migrate GameData cũ → Slot 1.");
                }
                else if (task.IsFaulted || task.IsCanceled)
                {
                    _lastCloudLoadFailed = true;
                    LoadGameLocal();
                }
                else
                {
                    LoadGameLocal();
                }

                onLoaded?.Invoke();
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
            Data = new GameData();

            try
            {
                if (SaveSlotStorage.TryLoadSlotFile(ActiveSlotIndex, Data))
                {
                    Data.hasSave = true;
                    Data.slotIndex = ActiveSlotIndex;
                    Data.EnsureLists();
                    Debug.Log($"[Save System] Tải local Slot {ActiveSlotIndex} thành công. RAM: playerHealth={Data.playerHealth}");
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
                        Data.EnsureLists();
                        SaveGameLocal();
                        Debug.Log("[Save System] Đã migrate save_data.json → save_slot_1.json");
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
            Data = new GameData();

            try
            {
                if (SaveSlotStorage.TryLoadBackupFile(ActiveSlotIndex, Data))
                {
                    Data.hasSave = true;
                    Data.slotIndex = ActiveSlotIndex;
                    Data.EnsureLists();
                    Debug.Log("[Save System] Đã khôi phục thành công từ file Backup.");
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
            Data.EnsureLists();
            HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
        }

        internal bool UsesGoogleCloudSaves()
        {
            return _user != null && !_user.IsAnonymous && _dbRef != null;
        }

        internal bool CloudSlotHasSave(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            return _cloudSlotIndexReady && _cloudSlotExists[slotIndex - 1];
        }

        internal void MergeCloudPeek(int slotIndex, ref GameData peek)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            GameData cloud = _cloudSlotPeeks[slotIndex - 1];
            if (cloud == null || !_cloudSlotExists[slotIndex - 1])
                return;

            if (peek == null)
            {
                peek = cloud;
                return;
            }

            if (cloud.totalPlayTimeSeconds > peek.totalPlayTimeSeconds)
                peek.totalPlayTimeSeconds = cloud.totalPlayTimeSeconds;
            if (string.IsNullOrEmpty(peek.lastPlayedAtUtc) && !string.IsNullOrEmpty(cloud.lastPlayedAtUtc))
                peek.lastPlayedAtUtc = cloud.lastPlayedAtUtc;
        }

        internal void RememberCloudSlot(int slotIndex, GameData data)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            _cloudSlotExists[slotIndex - 1] = data != null && data.hasSave;
            _cloudSlotPeeks[slotIndex - 1] = data;
        }

        internal void ClearCloudSlotCache(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            _cloudSlotExists[slotIndex - 1] = false;
            _cloudSlotPeeks[slotIndex - 1] = null;
        }

        internal void InvalidateCloudSlotIndex()
        {
            _cloudSlotIndexReady = false;
            _cloudSlotIndexLoading = false;
            _cloudSlotIndexSerial++;
            for (int i = 0; i < SlotCount; i++)
            {
                _cloudSlotExists[i] = false;
                _cloudSlotPeeks[i] = null;
            }
        }

        public void RefreshCloudSlotIndex(Action onComplete)
        {
            if (onComplete != null)
                _cloudSlotIndexWaiters.Add(onComplete);

            if (_cloudSlotIndexLoading)
                return;

            if (AuthSession.IsGuest && (_user == null || _user.IsAnonymous))
            {
                _cloudSlotIndexReady = true;
                CompleteCloudSlotIndex();
                return;
            }

            if (_isFirebaseInitializing)
            {
                _cloudSlotIndexLoading = true;
                StartCoroutine(WaitAndRefreshCloudSlotIndex());
                return;
            }

            FetchCloudSlotIndex();
        }

        private System.Collections.IEnumerator WaitAndRefreshCloudSlotIndex()
        {
            while (_isFirebaseInitializing)
                yield return null;

            FetchCloudSlotIndex();
        }

        private void FetchCloudSlotIndex()
        {
            if (!UsesGoogleCloudSaves())
            {
                _cloudSlotIndexLoading = false;
                _cloudSlotIndexReady = true;
                CompleteCloudSlotIndex();
                return;
            }

            _cloudSlotIndexLoading = true;
            int serial = ++_cloudSlotIndexSerial;
            _dbRef.Child("users").Child(_user.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (serial != _cloudSlotIndexSerial)
                    return;

                _cloudSlotIndexLoading = false;
                if (task.IsFaulted || task.IsCanceled || task.Result == null)
                {
                    Debug.LogWarning("[Firebase] Không đọc được danh sách slot cloud. Giữ bản local.");
                    _cloudSlotIndexReady = true;
                    CompleteCloudSlotIndex();
                    return;
                }

                ApplyUserSnapshotToCloudIndex(task.Result);
                _cloudSlotIndexReady = true;
                Debug.Log("[Firebase] Đã đồng bộ trạng thái 4 slot từ cloud.");
                CompleteCloudSlotIndex();
            });
        }

        private void ApplyUserSnapshotToCloudIndex(DataSnapshot userSnap)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _cloudSlotExists[i] = false;
                _cloudSlotPeeks[i] = null;
            }

            if (userSnap == null || !userSnap.Exists)
                return;

            DataSnapshot slotsSnap = userSnap.Child("slots");
            if (slotsSnap != null && slotsSnap.Exists)
            {
                for (int slot = 1; slot <= SlotCount; slot++)
                    TryReadCloudSlotSnapshot(slotsSnap.Child(slot.ToString()), slot);
            }

            if (!_cloudSlotExists[0])
                TryReadCloudSlotSnapshot(userSnap.Child("GameData"), 1);
        }

        private void TryReadCloudSlotSnapshot(DataSnapshot slotSnap, int slotIndex)
        {
            if (slotSnap == null || !slotSnap.Exists)
                return;

            DataSnapshot source = slotSnap.Child("GameData");
            if (source == null || !source.Exists)
                source = slotSnap;

            string json = source.GetRawJsonValue();
            if (string.IsNullOrEmpty(json) || json == "null")
                return;

            try
            {
                GameData data = SaveCrypto.ParseGameData(json);
                if (data == null)
                {
                    _cloudSlotExists[slotIndex - 1] = true;
                    return;
                }

                data.hasSave = true;
                data.slotIndex = slotIndex;
                _cloudSlotExists[slotIndex - 1] = true;
                _cloudSlotPeeks[slotIndex - 1] = data;
            }
            catch (Exception e)
            {
                _cloudSlotExists[slotIndex - 1] = true;
                Debug.LogWarning($"[Firebase] Slot {slotIndex} cloud JSON lỗi: {e.Message}");
            }
        }

        private void CompleteCloudSlotIndex()
        {
            var waiters = _cloudSlotIndexWaiters.ToArray();
            _cloudSlotIndexWaiters.Clear();
            for (int i = 0; i < waiters.Length; i++)
                waiters[i]?.Invoke();
        }
    }
}
