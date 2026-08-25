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

        public static DataManager Instance { get; private set; }

        public GameData Data = new GameData();
        public int ActiveSlotIndex { get; private set; } = 1;

        [Header("Debug / Test")]
        [Tooltip("Chi Editor: bat = giu chìa từ save khi Play. Tat (mac dinh) = moi lan Play chìa ve 0.")]
        [SerializeField] private bool keepSavedKeysWhenPlayInEditor = false;

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

        /// <summary>Scene mới đang hồi sinh / Continue — PlayerHealth không được ghi đè máu save bằng max.</summary>
        public bool IsApplyingSpawnRestore => _pendingRespawnApply;

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
            if (!ShouldTrackPlayTime())
                return;

            Data.totalPlayTimeSeconds += Time.unscaledDeltaTime;
            _playTimeDirty = true;
            _playTimeSaveTimer += Time.unscaledDeltaTime;
            if (_playTimeSaveTimer >= 60f)
                FlushPlayTimeIfNeeded();
        }

        private void OnApplicationQuit()
        {
            FlushPlayTimeIfNeeded();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                FlushPlayTimeIfNeeded();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "mainMenu" || scene.name == SelectLevelScene)
                FlushPlayTimeIfNeeded();
        }

        private bool ShouldTrackPlayTime()
        {
            if (!Application.isPlaying || Data == null || !Data.hasSave)
                return false;

            string scene = SceneManager.GetActiveScene().name;
            return scene != "mainMenu" && scene != SelectLevelScene;
        }

        private void FlushPlayTimeIfNeeded()
        {
            if (!_playTimeDirty || Data == null || !Data.hasSave)
                return;
            if (_isRespawning || Data.playerHealth <= 0)
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

            _slotEnterBusy = true;
            if (IsWaitingForCloudSlots)
            {
                int pendingSlot = slotIndex;
                RefreshCloudSlotIndex(() =>
                {
                    _slotEnterBusy = false;
                    SelectSlotAndEnter(pendingSlot);
                });
                return;
            }

            ActiveSlotIndex = slotIndex;
            SaveSlotStorage.SetActiveSlotIndex(slotIndex);

            LoadSlot(slotIndex, () =>
            {
                _slotEnterBusy = false;
                if (Data != null && Data.hasSave)
                {
                    ApplyChapterProgressFromLoadedData();
                    TouchLastPlayed();
                    SaveGame();
                    LoadSceneSafe(SelectLevelScene);
                    return;
                }

                if (!AuthSession.IsGuest && (!_isFirebaseReady || _lastCloudLoadFailed || CloudSlotHasSave(slotIndex)))
                {
                    Debug.LogWarning("[Save System] Cloud chưa chắc trống — không tạo save mới để tránh đè dữ liệu Google.");
                    return;
                }

                CreateNewSave(slotIndex);
                LoadSceneSafe(NewGameTutorialScene);
            });
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
                checkpointClearedRooms = new List<string>(),
                checkpointUnlockedDoors = new List<string>(),
                checkpointCollectedKeyPickupIds = new List<string>(),
            };

            ChapterProgress.ResetForNewSave();
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

        public void DeleteSave(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            SaveSlotStorage.DeleteLocalSlot(slotIndex);
            DeleteCloudSlot(slotIndex);
            ClearCloudSlotCache(slotIndex);

            if (ActiveSlotIndex == slotIndex)
            {
                Data = new GameData { slotIndex = slotIndex, hasSave = false };
                ChapterProgress.ResetForNewSave();
            }

            Debug.Log($"[Save System] Đã xóa Slot {slotIndex}.");
        }

        public void AbandonInProgress()
        {
            if (Data == null) return;
            Data.ClearInProgressWorldState();
            Data.targetSpawnID = "";
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
            SaveGame();
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

            string sceneToLoad = Data.checkpointScene;
            _pendingRespawnApply = true;
            _pendingContinueRestoreHealth = true;

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

            if (Data != null)
            {
                Data.RestoreCheckpointWorldState();
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                SaveGame();
            }

            _pendingRespawnApply = true;

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

        public void PrepareForNewScene()
        {
            HeartOfTheNight.Rooms.PlayerKeyInventory.ClearKeyCountsForNewScene();
        }
    }
}
