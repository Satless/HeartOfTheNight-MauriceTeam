using System;
using System.Collections.Generic;
using HeartOfTheNight.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeartOfTheNight.Hung
{
    public partial class DataManager : MonoBehaviour
    {
        public const int SlotCount = 4;
        public const string SelectLevelScene = "SelectLevel";
        public const string NewGameTutorialScene = "Khanh_Level0-1";
        public const string ExistingGoogleAccountNotice = "EXISTING_GOOGLE_ACCOUNT";
        public const string SlotEnterBlockedMessage =
            "Không tạo save mới — cloud chưa chắc trống.\n\n" +
            "Firebase chưa sẵn hoặc tải slot thất bại.\n" +
            "Kiểm tra mạng rồi thử lại.";

        public static DataManager Instance { get; private set; }

        public GameData Data = new GameData();
        public int ActiveSlotIndex { get; private set; } = 1;

        [Header("Checkpoint")]
        [Tooltip("Cho anim chết chạy trước khi fade + load lại scene.")]
        [SerializeField] private float respawnDelay = 1.2f;

        [Header("Google OAuth (Editor / Windows)")]
        [Tooltip("Firebase Console → Authentication → Google → Web client ID")]
        [SerializeField] private string googleWebClientId;
        [Tooltip("Google Cloud Console → Credentials → Web client → Client secret")]
        [SerializeField] private string googleWebClientSecret;
        [SerializeField] private int googleLoopbackPort = GoogleDesktopOAuth.DefaultPort;

        private bool _pendingRespawnApply;
        private bool _pendingContinueRestoreHealth;
        private bool _isRespawning;
        private bool _playTimeDirty;
        private float _playTimeSaveTimer;
        private bool _slotEnterBusy;
        private int _slotEnterSerial;
        private bool _slotDeleteBusy;
        private string _activeSceneName;

        /// <summary>Scene.name cấp phát string mới mỗi lần gọi — cache lại để Update không sinh rác.</summary>
        private string ActiveSceneName
        {
            get
            {
                if (_activeSceneName == null)
                    _activeSceneName = SceneManager.GetActiveScene().name;
                return _activeSceneName;
            }
        }

        /// <summary>Scene mới đang hồi sinh / Continue — PlayerHealth không được ghi đè máu save bằng max.</summary>
        public bool IsApplyingSpawnRestore => _pendingRespawnApply;

        public bool IsDeletingSave => _slotDeleteBusy;

        internal Firebase.Auth.FirebaseUser FirebaseUser => _user;

        public static DataManager EnsureExists()
        {
            if (Instance != null)
                return Instance;

            var prefab = Resources.Load<GameObject>("Data/DataManager");
            if (prefab != null)
            {
                var spawned = Instantiate(prefab);
                spawned.name = "DataManager";
                return Instance;
            }

            Debug.LogWarning("[Save System] Không tìm thấy Resources/Data/DataManager. Tạo trống — Google OAuth trên Windows sẽ thiếu Client Secret nếu không đi từ AuthScene.");
            var go = new GameObject("DataManager");
            return go.AddComponent<DataManager>();
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                    DontDestroyOnLoad(gameObject);
                ActiveSlotIndex = SaveSlotStorage.GetActiveSlotIndex();

                if (Application.isPlaying)
                {
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    InitializeFirebase();
                }
            }
            else
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            float delta = Time.unscaledDeltaTime;
            TickLevelTimer(ActiveSceneName, delta);

            if (_levelTimerPaused || !ShouldTrackPlayTime())
                return;

            Data.totalPlayTimeSeconds += delta;
            _playTimeDirty = true;
            _playTimeSaveTimer += delta;
            if (_playTimeSaveTimer >= 60f)
                FlushPlayTimeIfNeeded();
        }

        private void OnApplicationQuit()
        {
            PersistOnLeaveApp();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                PersistOnLeaveApp();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _activeSceneName = SceneManager.GetActiveScene().name;

            if (_activeSceneName == "mainMenu"
                || _activeSceneName == SelectLevelScene
                || StoryFlow.IsCinematic(_activeSceneName))
                FlushPlayTimeIfNeeded();

            SyncLevelTimerToLoadedScene(_activeSceneName);
        }

        private bool ShouldTrackPlayTime()
        {
            if (!Application.isPlaying || Data == null || !Data.hasSave)
                return false;

            return IsLevelScene(ActiveSceneName);
        }

        /// <summary>
        /// Alt-tab / thoát app: ghi snapshot đã commit, không rollback RAM đang chơi.
        /// </summary>
        private void PersistOnLeaveApp()
        {
            if (Data != null && Data.hasSave && IsLevelScene(ActiveSceneName))
            {
                PersistCommittedWorldToDiskKeepLive();
                return;
            }

            FlushPlayTimeIfNeeded();
        }

        /// <summary>
        /// Đang trong màn: RAM đang chơi — không được LoadGame đè, kể cả khi hasSave còn false.
        /// </summary>
        internal bool ShouldPreserveLiveRamSave()
        {
            return Application.isPlaying && IsLevelScene(ActiveSceneName);
        }

        /// <summary>
        /// Ghi chìa/phòng/cửa đã commit xuống đĩa. HUD và map giữ nguyên bản đang chơi.
        /// </summary>
        private void PersistCommittedWorldToDiskKeepLive()
        {
            if (Data == null || !Data.hasSave)
                return;
            if (!Data.hasCheckpointWorldState)
                return;

            var live = Data.CopyLiveWorld();
            var liveTimers = Data.CopyLiveScenePlayTimes();
            Data.RestoreCheckpointWorldState();
            Data.RestoreCheckpointScenePlayTimes();
            SaveGame();
            Data.ApplyLiveWorld(live);
            Data.ApplyLiveScenePlayTimes(liveTimers);
            RebindLevelTimerAfterListReplace();
        }

        private void FlushPlayTimeIfNeeded()
        {
            if (!_playTimeDirty || Data == null || !Data.hasSave)
                return;
            if (_isRespawning || Data.playerHealth <= 0)
                return;
            if (IsLevelScene(ActiveSceneName))
                return;

            Data.lastPlayedAtUtc = DateTime.UtcNow.ToString("o");
            SaveGameLocal();
            _playTimeDirty = false;
            _playTimeSaveTimer = 0f;
        }

        public static string GetSlotSavePath(int slotIndex) => SaveSlotStorage.GetSlotSavePath(slotIndex);

        public static string GetSlotBackupPath(int slotIndex) => SaveSlotStorage.GetSlotBackupPath(slotIndex);

        public static bool HasSave(int slotIndex)
        {
            if (SaveSlotStorage.HasSave(slotIndex))
                return true;
            return Instance != null && Instance.CloudSlotHasSave(slotIndex);
        }

        public static int GetActiveSlotIndex() => SaveSlotStorage.GetActiveSlotIndex();

        public static bool TryPeekSlot(int slotIndex, out GameData peek)
        {
            SaveSlotStorage.TryPeekSlot(slotIndex, out peek);
            if (Instance != null)
                Instance.MergeCloudPeek(slotIndex, ref peek);
            return peek != null;
        }

        public bool HasInProgress()
        {
            return Data != null && Data.hasCheckpoint && !string.IsNullOrEmpty(Data.checkpointScene);
        }

        public void SelectSlotAndEnter(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            if (_slotEnterBusy)
                return;

            int enterGen = ++_slotEnterSerial;
            _slotEnterBusy = true;
            if (IsWaitingForCloudSlots)
            {
                int pendingSlot = slotIndex;
                RefreshCloudSlotIndex(() =>
                {
                    if (enterGen != _slotEnterSerial)
                        return;
                    _slotEnterBusy = false;
                    SelectSlotAndEnter(pendingSlot);
                });
                return;
            }

            ActiveSlotIndex = slotIndex;
            SaveSlotStorage.SetActiveSlotIndex(slotIndex);

            LoadSlot(slotIndex, () =>
            {
                if (enterGen != _slotEnterSerial)
                    return;

                _slotEnterBusy = false;
                if (Data != null && Data.hasSave)
                {
                    ApplyChapterProgressFromLoadedData();
                    TouchLastPlayed();
                    SaveGame();
                    HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                    LoadSceneSafe(SelectLevelScene);
                    return;
                }

                if (!AuthSession.IsGuest && (!_isFirebaseReady || _lastCloudLoadFailed || CloudSlotHasSave(slotIndex)))
                {
                    NotifySlotEnterFailed(SlotEnterBlockedMessage);
                    return;
                }

                CreateNewSave(slotIndex);
                LoadSceneSafe(StoryFlow.Story1);
            });
        }

        private void AbortSlotEnter()
        {
            _slotEnterSerial++;
            _slotEnterBusy = false;
        }

        private void NotifySlotEnterFailed(string message)
        {
            Debug.LogWarning("[Save System] " + message);
            SaveSlotFlowUI.PresentBlockedMessage(message);
        }

        public void CreateNewSave(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            ActiveSlotIndex = slotIndex;
            SaveSlotStorage.SetActiveSlotIndex(slotIndex);

            string now = DateTime.UtcNow.ToString("o");
            Data = new GameData
            {
                slotIndex = slotIndex,
                hasSave = true,
                createdAtUtc = now,
                lastPlayedAtUtc = now,
                playerHealth = 100,
                maxUnlockedLevel = 1,
                currentScene = NewGameTutorialScene,
                hasCheckpoint = false,
                hasCheckpointWorldState = false,
                totalPlayTimeSeconds = 0f,
                scenePlayTimes = new List<ScenePlayTimeEntry>(),
                clearedRooms = new List<string>(),
                unlockedDoors = new List<string>(),
                collectedKeyPickupIds = new List<string>(),
                foundSecrets = new List<string>(),
                unlockedWeapons = new bool[] { true, false, false, false },
                checkpointClearedRooms = new List<string>(),
                checkpointUnlockedDoors = new List<string>(),
                checkpointCollectedKeyPickupIds = new List<string>(),
                checkpointFoundSecrets = new List<string>(),
            };

            ChapterProgress.ResetForNewSave();
            Data.CaptureCheckpointWorldState();
            SaveGame();
            RememberCloudSlot(slotIndex, Data);
            Debug.Log($"[Save System] Tạo save mới Slot {slotIndex} → {NewGameTutorialScene}");
        }

        private void ApplyChapterProgressFromLoadedData()
        {
            if (Data != null && Data.hasSave)
                ChapterProgress.ApplyFromSave(Data);
        }

        public void LoadSlot(int slotIndex, Action onLoaded = null)
        {
            ActiveSlotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            SaveSlotStorage.SetActiveSlotIndex(ActiveSlotIndex);
            LoadGame(() =>
            {
                ApplyChapterProgressFromLoadedData();
                onLoaded?.Invoke();
            });
        }

        public void DeleteSave(int slotIndex, Action<bool> onComplete = null)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            if (_slotDeleteBusy)
            {
                onComplete?.Invoke(false);
                return;
            }

            string accountKey = SaveSlotStorage.GetAccountSaveKey();
            string userId = CurrentCloudUserId();

            if (string.IsNullOrEmpty(userId))
            {
                ApplyLocalDelete(slotIndex, accountKey);
                onComplete?.Invoke(true);
                return;
            }

            if (ShouldDeferCloudDelete(slotIndex))
            {
                _slotDeleteBusy = true;
                _deleteWhenIdleSlot = slotIndex;
                _deleteWhenIdleAccountKey = accountKey;
                _deleteWhenIdleUserId = userId;
                _deleteWhenIdleCallback = onComplete;
                return;
            }

            BeginCloudDelete(slotIndex, userId, accountKey, onComplete);
        }

        private string CurrentCloudUserId()
        {
            return _user != null && !_user.IsAnonymous ? _user.UserId : null;
        }

        private void BeginCloudDelete(int slotIndex, string userId, string accountKey, Action<bool> onComplete)
        {
            _slotDeleteBusy = true;
            int pending = slotIndex;
            int deleteGen = _cloudDeleteSerial;
            string key = string.IsNullOrEmpty(accountKey) ? SaveSlotStorage.GetAccountSaveKey() : accountKey;
            DeleteCloudSlot(pending, userId, ok =>
            {
                if (deleteGen != _cloudDeleteSerial)
                    return;

                _slotDeleteBusy = false;
                if (!ok)
                {
                    Debug.LogError($"[Save System] Xóa Slot {pending} trên Cloud thất bại — giữ bản máy để save không bị kéo lại.");
                    onComplete?.Invoke(false);
                    return;
                }

                ApplyLocalDelete(pending, key);
                onComplete?.Invoke(true);
            });
        }

        private void ApplyLocalDelete(int slotIndex, string accountKey)
        {
            if (string.IsNullOrEmpty(accountKey))
                accountKey = SaveSlotStorage.GetAccountSaveKey();

            SaveSlotStorage.DeleteLocalSlot(slotIndex, accountKey);

            bool sameAccount = accountKey == SaveSlotStorage.GetAccountSaveKey();
            if (sameAccount)
            {
                ClearCloudSlotCache(slotIndex);
                ChapterProgress.ResetForSlot(slotIndex);
                if (ActiveSlotIndex == slotIndex)
                    Data = new GameData { slotIndex = slotIndex, hasSave = false };
            }

            Debug.Log($"[Save System] Đã xóa Slot {slotIndex} ({accountKey}).");
        }

        public void AbandonInProgress()
        {
            if (Data == null) return;
            Data.ClearInProgressWorldState();
            Data.targetSpawnID = "";
            SaveGame();
        }

        /// <summary>
        /// Pause HOME / EXIT: rollback RAM về snapshot (cửa checkpoint hoặc lúc vào màn), rồi mới ghi file.
        /// Chưa có snapshot thì không ghi chìa/phòng đang dở.
        /// </summary>
        public void SaveBeforeLeaveLevel()
        {
            if (Data == null || !Data.hasSave)
                return;

            if (Data.hasCheckpointWorldState)
            {
                Data.RestoreCheckpointWorldState();
                Data.RestoreCheckpointScenePlayTimes();
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                SaveGame();
                return;
            }

            if (IsLevelScene(ActiveSceneName))
                return;

            SaveGame();
        }

        public bool IsRoomCleared(string roomId)
        {
            return Data != null && Data.IsRoomCleared(roomId);
        }

        public void MarkRoomCleared(string roomId)
        {
            if (Data == null) Data = new GameData();
            Data.MarkRoomCleared(roomId);
        }

        public void ContinueFromCheckpoint()
        {
            if (!HasInProgress())
            {
                Debug.LogWarning("[Save System] Không có màn đang chơi dở để Continue.");
                return;
            }

            if (_isRespawning) return;
            _isRespawning = true;
            StartCoroutine(ContinueFromCheckpointRoutine());
        }

        private System.Collections.IEnumerator ContinueFromCheckpointRoutine()
        {
            Time.timeScale = 1f;

            if (ScreenFader.Instance != null)
                yield return ScreenFader.Instance.FadeOut();

            if (Data != null)
            {
                Data.RestoreCheckpointWorldState();
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                SaveGame();
            }

            string sceneToLoad = Data != null ? Data.checkpointScene : "";
            _pendingRespawnApply = true;
            _pendingContinueRestoreHealth = true;
            KeepLevelTimeAcrossNextLoad();

            if (Data.hasCheckpointWorldState && Data.checkpointPlayerHealth > 0)
                Data.playerHealth = Data.checkpointPlayerHealth;

            if (!string.IsNullOrEmpty(Data.checkpointSpawnID))
                LevelEntrance.SetPendingSpawn(Data.checkpointSpawnID);
            else
                LevelEntrance.ClearPendingSpawn();

            if (ScreenFader.Instance != null)
                ScreenFader.Instance.LoadSceneWithLoading(sceneToLoad);
            else
                SceneManager.LoadScene(sceneToLoad);

            _isRespawning = false;
        }

        private void TouchLastPlayed()
        {
            if (Data == null) return;
            Data.hasSave = true;
            Data.slotIndex = ActiveSlotIndex;
            Data.lastPlayedAtUtc = DateTime.UtcNow.ToString("o");
            if (string.IsNullOrEmpty(Data.createdAtUtc))
                Data.createdAtUtc = Data.lastPlayedAtUtc;
        }

        private static void LoadSceneSafe(string sceneName)
        {
            if (ScreenFader.Instance != null)
                ScreenFader.Instance.LoadSceneWithLoading(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }

        public void SaveCheckpoint(string sceneName, string spawnId, Vector3 worldPosition, int health = -1)
        {
            if (Data == null) Data = new GameData();

            Data.hasCheckpoint = true;
            Data.checkpointScene = sceneName ?? "";
            Data.checkpointSpawnID = spawnId ?? "";
            Data.checkpointPosition = worldPosition;
            Data.currentScene = Data.checkpointScene;
            Data.targetSpawnID = Data.checkpointSpawnID;
            Data.playerPosition = worldPosition;
            if (health > 0)
                Data.playerHealth = health;

            Data.CaptureCheckpointWorldState();
            SaveGame();
            Debug.Log($"[Checkpoint] Đã lưu cửa: scene={Data.checkpointScene}, spawnId={Data.checkpointSpawnID}, pos={worldPosition}");
        }

        public void RespawnAtCheckpoint()
        {
            if (_isRespawning) return;
            _isRespawning = true;
            StartCoroutine(RespawnAtCheckpointRoutine());
        }

        private System.Collections.IEnumerator RespawnAtCheckpointRoutine()
        {
            Time.timeScale = 1f;

            if (ScreenFader.Instance != null)
                yield return ScreenFader.Instance.FadeOut();

            string sceneToLoad = SceneManager.GetActiveScene().name;
            if (Data != null && Data.hasCheckpoint && !string.IsNullOrEmpty(Data.checkpointScene))
                sceneToLoad = Data.checkpointScene;

            if (Data != null && Data.hasCheckpointWorldState)
            {
                Data.RestoreCheckpointWorldState();
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                SaveGame();
            }

            _pendingRespawnApply = true;
            KeepLevelTimeAcrossNextLoad();

            if (Data != null && Data.hasCheckpoint && !string.IsNullOrEmpty(Data.checkpointSpawnID))
                LevelEntrance.SetPendingSpawn(Data.checkpointSpawnID);
            else
                LevelEntrance.ClearPendingSpawn();

            if (ScreenFader.Instance != null)
                ScreenFader.Instance.LoadSceneWithLoading(sceneToLoad);
            else
                SceneManager.LoadScene(sceneToLoad);

            _isRespawning = false;
        }

        public void TryApplyPendingRespawn()
        {
            if (!_pendingRespawnApply) return;
            _pendingRespawnApply = false;

            GameObject player = LevelEntrance.FindPlayerRoot();
            if (player == null) return;

            bool alreadySpawnedByEntrance = Data != null
                && !string.IsNullOrEmpty(Data.checkpointSpawnID)
                && string.IsNullOrEmpty(LevelEntrance.PendingSpawnID);

            if (!alreadySpawnedByEntrance
                && Data != null
                && Data.hasCheckpoint
                && Data.checkpointPosition != Vector3.zero)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false;
                }

                player.transform.position = Data.checkpointPosition;
                if (Camera.main != null)
                {
                    Vector3 cam = Camera.main.transform.position;
                    Camera.main.transform.position = new Vector3(
                        Data.checkpointPosition.x,
                        Data.checkpointPosition.y,
                        cam.z);
                }

                if (rb != null) rb.simulated = true;
            }

            var hp = player.GetComponent<HeartOfTheNight.Player.PlayerHealth>();
            if (hp != null)
            {
                if (_pendingContinueRestoreHealth)
                {
                    _pendingContinueRestoreHealth = false;
                    hp.SyncHealthFromSave();
                }
                else
                {
                    hp.HealToFull();
                }
            }
        }

        /// <summary>
        /// Vào màn từ Select Level / cửa qua màn: tui chìa = 0, data của scene đích reset (replay).
        /// Không gọi khi chết / Continue.
        /// </summary>
        public void PrepareForNewScene(string destinationSceneName = "")
        {
            HeartOfTheNight.Rooms.PlayerKeyInventory.ClearKeyCountsForNewScene();
            if (Data == null) return;
            Data.EnsureLists();
            if (!string.IsNullOrEmpty(destinationSceneName))
                Data.ClearSceneLocalProgress(destinationSceneName);
            Data.CaptureCheckpointWorldState();
            if (Data.hasSave)
                SaveGame();
        }

        /// <summary>Cửa qua màn không phải checkpoint — xóa in-progress màn cũ rồi ghi slot.</summary>
        public void ClearCheckpointAfterLeavingLevel()
        {
            if (Data == null) return;
            Data.ClearCheckpointFlags();
            Data.targetSpawnID = "";
            SaveGame();
        }

        /// <summary>
        /// Unlock Select Level: ghi maxUnlockedLevel xuống đĩa mà không dump chìa đang nhặt dở.
        /// Tránh crash lúc YOU WIN làm ApplyFromSave khóa lại màn vừa mở.
        /// </summary>
        public void PersistUnlockProgress()
        {
            if (Data == null || !Data.hasSave)
                return;

            if (ShouldPreserveLiveRamSave() && Data.hasCheckpointWorldState)
            {
                PersistCommittedWorldToDiskKeepLive();
                return;
            }

            SaveGame();
        }

        /// <summary>
        /// Level Complete → Home: chốt world lúc thắng, hết màn đang dở.
        /// Select Level không hỏi Continue màn vừa YOU WIN.
        /// </summary>
        public void CommitFinishedLevelAndLeave()
        {
            if (Data == null || !Data.hasSave)
                return;

            Data.CaptureCheckpointWorldState();
            Data.ClearCheckpointFlags();
            Data.targetSpawnID = "";
            SaveGame();
        }
    }
}
