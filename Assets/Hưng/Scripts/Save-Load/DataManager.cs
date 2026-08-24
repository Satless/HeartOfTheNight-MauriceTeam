using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

namespace HeartOfTheNight.Hung
{
    [System.Serializable]
    public class ScenePlayTimeEntry
    {
        public string sceneName;
        /// <summary>Tổng giây đã chơi trong scene này (sẽ dùng ở bước sau).</summary>
        public float playSeconds;
    }

    [System.Serializable]
    public class GameData
    {
        public int slotIndex = 1;
        public bool hasSave;
        public string createdAtUtc;
        public string lastPlayedAtUtc;

        public int playerHealth;
        public int playerCoin;
        public string currentScene;
        public string targetSpawnID; 
        public Vector3 playerPosition; // Thêm vị trí Player
        public List<string> clearedRooms = new List<string>();

        // Checkpoint gắn cửa: chết / Continue về cửa đã kích hoạt gần nhất
        // hasCheckpoint ≈ "đang chơi dở màn" trong sơ đồ Continue
        public bool hasCheckpoint;
        public string checkpointScene;
        public string checkpointSpawnID;
        public Vector3 checkpointPosition;

        public int maxUnlockedLevel = 1; // Mặc định luôn mở Level 1

        // Chìa khóa cửa (Blue/Red) + cửa đã mở khóa vĩnh viễn
        public int blueKeys;
        public int redKeys;
        public bool collectedBlueKey; // true sau khi nhặt lần đầu (HUD)
        public bool collectedRedKey;
        public List<string> unlockedDoors = new List<string>();
        /// <summary>Id từng KeyPickup đã nhặt trên map (nhiều key cùng màu / cùng scene).</summary>
        public List<string> collectedKeyPickupIds = new List<string>();

        /// <summary>Tổng thời gian chơi cả slot (giây) — UI sẽ hiện ở bước sau.</summary>
        public float totalPlayTimeSeconds;
        /// <summary>Thời gian từng scene đã chinh phục — UI sẽ hiện ở bước sau.</summary>
        public List<ScenePlayTimeEntry> scenePlayTimes = new List<ScenePlayTimeEntry>();
    }

    // Class quản lý sống xuyên Scene
    public class DataManager : MonoBehaviour
    {
        public const int SlotCount = 4;
        public const string SelectLevelScene = "SelectLevel";
        public const string NewGameTutorialScene = "Khanh_Level0-1";

        public static DataManager Instance { get; private set; }

        public GameData Data = new GameData();
        public int ActiveSlotIndex { get; private set; } = 1;

        [Header("Debug / Test")]
        [Tooltip("Chi Editor: bat = giu chìa từ save khi Play. Tat (mac dinh) = moi lan Play chìa ve 0.")]
        [SerializeField] private bool keepSavedKeysWhenPlayInEditor = false;

        [Header("Checkpoint")]
        [Tooltip("Cho anim chết chạy trước khi fade + load lại scene.")]
        [SerializeField] private float respawnDelay = 1.2f;

        private bool _pendingRespawnApply;
        private bool _pendingContinueRestoreHealth;
        private bool _isRespawning;
        private bool _playTimeDirty;
        private float _playTimeSaveTimer;

        private string SavePath => GetSlotSavePath(ActiveSlotIndex);
        private string BackupPath => GetSlotBackupPath(ActiveSlotIndex);
        private string TempPath => GetSlotTempPath(ActiveSlotIndex);

        private FirebaseAuth _auth;
        private FirebaseUser _user;
        private DatabaseReference _dbRef;
        private bool _isFirebaseReady = false;
        private bool _isFirebaseInitializing = false; // Thêm cờ để biết đang khởi tạo

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
                ActiveSlotIndex = Mathf.Clamp(PlayerPrefs.GetInt("Save.ActiveSlot", 1), 1, SlotCount);

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

            Data.lastPlayedAtUtc = DateTime.UtcNow.ToString("o");
            SaveGameLocal();
            _playTimeDirty = false;
            _playTimeSaveTimer = 0f;
        }

        public static string GetSlotSavePath(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.json");
        }

        public static string GetSlotBackupPath(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.bak");
        }

        private static string GetSlotTempPath(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.tmp");
        }

        /// <summary>Slot đã có file save local (hoặc legacy save_data.json với slot 1).</summary>
        public static bool HasSave(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            if (File.Exists(GetSlotSavePath(slotIndex)))
                return true;

            // Migrate: save cũ 1 file → coi như Slot 1
            if (slotIndex == 1 && File.Exists(Path.Combine(Application.persistentDataPath, "save_data.json")))
                return true;

            return false;
        }

        /// <summary>
        /// Đọc JSON local của slot để hiện UI. Không đổi ActiveSlot / Data đang chơi.
        /// Gộp play time lớn nhất giữa RAM, file chính và file .bak (tránh cloud đè mất giờ chơi Slot 1).
        /// </summary>
        public static bool TryPeekSlot(int slotIndex, out GameData peek)
        {
            peek = null;
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);

            TryReadSlotFromDisk(slotIndex, out GameData disk);
            float diskPlayTime = disk != null ? disk.totalPlayTimeSeconds : 0f;

            if (Instance != null
                && Instance.ActiveSlotIndex == slotIndex
                && Instance.Data != null
                && Instance.Data.hasSave)
            {
                if (diskPlayTime > Instance.Data.totalPlayTimeSeconds)
                    Instance.Data.totalPlayTimeSeconds = diskPlayTime;
                peek = Instance.Data;
                return true;
            }

            peek = disk;
            return peek != null;
        }

        private static bool TryReadSlotFromDisk(int slotIndex, out GameData peek)
        {
            peek = null;
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);

            TryLoadGameDataFile(GetSlotSavePath(slotIndex), ref peek);
            TryLoadGameDataFile(GetSlotBackupPath(slotIndex), ref peek);

            if (slotIndex == 1)
            {
                string legacy = Path.Combine(Application.persistentDataPath, "save_data.json");
                TryLoadGameDataFile(legacy, ref peek);
            }

            return peek != null;
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

        private void KeepBetterLocalPlayTime()
        {
            if (Data == null)
                return;

            if (!TryReadSlotFromDisk(ActiveSlotIndex, out GameData local) || local == null)
                return;

            if (local.totalPlayTimeSeconds > Data.totalPlayTimeSeconds)
                Data.totalPlayTimeSeconds = local.totalPlayTimeSeconds;
        }

        public bool HasInProgress()
        {
            return Data != null && Data.hasCheckpoint && !string.IsNullOrEmpty(Data.checkpointScene);
        }

        /// <summary>
        /// Sơ đồ bước 1: chọn slot → trống = new game (Level 0-1), có save = Select Level.
        /// Popup Continue/Bỏ làm ở bước sau.
        /// </summary>
        public void SelectSlotAndEnter(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            ActiveSlotIndex = slotIndex;
            PlayerPrefs.SetInt("Save.ActiveSlot", slotIndex);
            PlayerPrefs.Save();

            if (!HasSave(slotIndex))
            {
                CreateNewSave(slotIndex);
                LoadSceneSafe(NewGameTutorialScene);
                return;
            }

            LoadSlot(slotIndex, () =>
            {
                TouchLastPlayed();
                SaveGame();
                LoadSceneSafe(SelectLevelScene);
            });
        }

        public void CreateNewSave(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            ActiveSlotIndex = slotIndex;
            PlayerPrefs.SetInt("Save.ActiveSlot", slotIndex);
            PlayerPrefs.Save();

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
                totalPlayTimeSeconds = 0f,
                scenePlayTimes = new List<ScenePlayTimeEntry>(),
                clearedRooms = new List<string>(),
                unlockedDoors = new List<string>(),
                collectedKeyPickupIds = new List<string>(),
            };

            ChapterProgress.ResetForNewSave();
            SaveGame();
            Debug.Log($"[Save System] Tạo save mới Slot {slotIndex} → {NewGameTutorialScene}");
        }

        public void LoadSlot(int slotIndex, Action onLoaded = null)
        {
            ActiveSlotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);
            PlayerPrefs.SetInt("Save.ActiveSlot", ActiveSlotIndex);
            PlayerPrefs.Save();
            LoadGame(onLoaded);
        }

        /// <summary>Xóa toàn bộ save của slot (local + cloud nếu có).</summary>
        public void DeleteSave(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, SlotCount);

            TryDeleteFile(GetSlotSavePath(slotIndex));
            TryDeleteFile(GetSlotBackupPath(slotIndex));
            TryDeleteFile(GetSlotTempPath(slotIndex));

            if (slotIndex == 1)
            {
                TryDeleteFile(Path.Combine(Application.persistentDataPath, "save_data.json"));
                TryDeleteFile(Path.Combine(Application.persistentDataPath, "save_data.bak"));
                TryDeleteFile(Path.Combine(Application.persistentDataPath, "save_data.tmp"));
            }

            if (_isFirebaseReady && _user != null && _dbRef != null)
            {
                _dbRef.Child("users").Child(_user.UserId).Child("slots").Child(slotIndex.ToString())
                    .RemoveValueAsync();
            }

            if (ActiveSlotIndex == slotIndex)
            {
                Data = new GameData { slotIndex = slotIndex, hasSave = false };
            }

            Debug.Log($"[Save System] Đã xóa Slot {slotIndex}.");
        }

        private static void TryDeleteFile(string path)
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

        /// <summary>Bỏ màn đang chơi dở — giữ unlock / tiến độ slot. Dùng ở bước Continue popup.</summary>
        public void AbandonInProgress()
        {
            if (Data == null) return;
            Data.hasCheckpoint = false;
            Data.checkpointScene = "";
            Data.checkpointSpawnID = "";
            Data.checkpointPosition = Vector3.zero;
            Data.targetSpawnID = "";
            SaveGame();
        }

        /// <summary>Sơ đồ: người chơi chọn Continue → load checkpoint (giống chết hồi sinh, nhưng giữ máu save).</summary>
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

        private void InitializeFirebase()
        {
            _isFirebaseInitializing = true;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _auth = FirebaseAuth.DefaultInstance;
                    _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                    SignInAnonymously();
                }
                else
                {
                    _isFirebaseInitializing = false;
                    Debug.LogError($"[Firebase] Không thể khởi tạo Firebase: {task.Result}. Fallback sang Local Load.");
                    LoadGameLocal(); 
                }
            });
        }

        private void SignInAnonymously()
        {
            // Kiểm tra xem máy này đã từng đăng nhập ẩn danh chưa (để tránh tạo UID mới liên tục)
            if (_auth.CurrentUser != null)
            {
                _user = _auth.CurrentUser;
                _isFirebaseReady = true;
                _isFirebaseInitializing = false;
                Debug.Log($"[Firebase] Đã nhớ tài khoản cũ! UID: {_user.UserId}");  
                LoadGameCloud();
                return;
            }

            _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                _isFirebaseInitializing = false;
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("[Firebase] Đăng nhập ẩn danh thất bại! Fallback sang Local Load.");
                    LoadGameLocal();
                    return;
                }

                _user = _auth.CurrentUser; 
                _isFirebaseReady = true;
                Debug.Log($"[Firebase] Đăng nhập ẩn danh thành công! UID: {_user.UserId}");
                
                // Sau khi đăng nhập thành công mới bắt đầu Load Data từ Cloud
                LoadGameCloud();
            });
        }

        // Gọi hàm này khi bấm F5
        public void SaveGame()
        {
            if (Data == null) Data = new GameData();
            Data.hasSave = true;
            Data.slotIndex = ActiveSlotIndex;
            Data.lastPlayedAtUtc = DateTime.UtcNow.ToString("o");
            _playTimeDirty = false;
            _playTimeSaveTimer = 0f;

            // Luôn lưu local làm lốp dự phòng
            SaveGameLocal();

            // Nếu Firebase đã kết nối, đẩy 1 bản copy lên Cloud
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

        private DatabaseReference GetSlotDbRef()
        {
            return _dbRef.Child("users").Child(_user.UserId).Child("slots").Child(ActiveSlotIndex.ToString()).Child("GameData");
        }

        /// <summary>
        /// Lưu cửa vừa đi qua làm điểm hồi sinh. Ghi local + cloud.
        /// </summary>
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

            SaveGame();
            Debug.Log($"[Checkpoint] Đã lưu cửa: scene={Data.checkpointScene}, spawnId={Data.checkpointSpawnID}, pos={worldPosition}");
        }

        /// <summary>
        /// Chết → chờ anim → reload scene tại checkpoint (hoặc điểm spawn mặc định nếu chưa qua cửa checkpoint).
        /// </summary>
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

        /// <summary>
        /// Gọi sau khi scene load (từ ScreenFader). Chỉ khi đang hồi sinh / Continue.
        /// </summary>
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
        /// Gọi trước khi LoadScene: chìa chỉ dùng trong scene hiện tại, túi về 0.
        /// Pickup đã nhặt / cửa đã mở vẫn persist.
        /// </summary>
        public void PrepareForNewScene()
        {
            HeartOfTheNight.Rooms.PlayerKeyInventory.ClearKeyCountsForNewScene();
        }

        // Gọi hàm này khi bấm F9. Có action callback để chờ mạng load xong mới dịch chuyển nhân vật
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

        private System.Collections.IEnumerator WaitAndLoadCloud(Action onLoaded)
        {
            // Chờ cho đến khi cờ Initializing tắt (nghĩa là Firebase đã kết nối xong hoặc thất bại)
            while (_isFirebaseInitializing)
            {
                yield return null;
            }
            
            // Chờ xong thì tự động gọi lại LoadGame (lúc này nó sẽ lọt vào if (_isFirebaseReady) hoặc else)
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
                        
                        // Tải cloud xong thì lưu đè xuống Local để làm backup cho lần sau mất mạng
                        SaveGameLocal();
                        ApplyEditorKeyResetIfNeeded();
                        HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                    }
                    else if (ActiveSlotIndex == 1)
                    {
                        // Migrate: cloud cũ users/{uid}/GameData → slot 1
                        TryLoadLegacyCloudThenLocal(onLoaded);
                        return;
                    }
                    else
                    {
                        Debug.Log($"[Firebase] Slot {ActiveSlotIndex} chưa có trên Cloud. Đang tải Local...");
                        LoadGameLocal();
                    }
                }
                
                // Báo cho TestSaveLoad biết là đã load xong (có thể dịch chuyển nhân vật)
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
                string json = JsonUtility.ToJson(Data, true);
                File.WriteAllText(TempPath, json);
                if (File.Exists(SavePath))
                {
                    if (File.Exists(BackupPath))
                        File.Delete(BackupPath);
                    File.Move(SavePath, BackupPath);
                }
                File.Move(TempPath, SavePath);
                Debug.Log($"[Save System] Đã lưu local thành công.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save System] Lỗi khi lưu local: {e.Message}");
            }
        }

        private void LoadGameLocal()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    if (Data == null) Data = new GameData();
                    JsonUtility.FromJsonOverwrite(json, Data);
                    Data.hasSave = true;
                    Data.slotIndex = ActiveSlotIndex;
                    Debug.Log($"[Save System] Tải local Slot {ActiveSlotIndex} thành công. RAM: playerHealth={Data.playerHealth}");
                    ApplyEditorKeyResetIfNeeded();
                    HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                    return; 
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Save System] File chính bị hỏng, tải từ Backup. Lỗi: {e.Message}");
                    LoadFromBackupLocal();
                }
            }
            else if (ActiveSlotIndex == 1)
            {
                // Migrate save_data.json cũ → Slot 1
                string legacy = Path.Combine(Application.persistentDataPath, "save_data.json");
                if (File.Exists(legacy))
                {
                    try
                    {
                        string json = File.ReadAllText(legacy);
                        if (Data == null) Data = new GameData();
                        JsonUtility.FromJsonOverwrite(json, Data);
                        Data.hasSave = true;
                        Data.slotIndex = 1;
                        SaveGameLocal();
                        Debug.Log("[Save System] Đã migrate save_data.json → save_slot_1.json");
                        ApplyEditorKeyResetIfNeeded();
                        HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Save System] Migrate legacy thất bại: {e.Message}");
                    }
                }

                LoadFromBackupLocal();
            }
            else
            {
                LoadFromBackupLocal();
            }
        }

        private void LoadFromBackupLocal()
        {
            if (File.Exists(BackupPath))
            {
                try
                {
                    string json = File.ReadAllText(BackupPath);
                    if (Data == null) Data = new GameData();
                    JsonUtility.FromJsonOverwrite(json, Data);
                    Debug.Log("[Save System] Đã khôi phục thành công từ file Backup.");
                    ApplyEditorKeyResetIfNeeded();
                    HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Save System] File Backup cũng bị lỗi! Tạo data mới hoàn toàn. Lỗi: {e.Message}");
                    Data = new GameData();
                    ApplyEditorKeyResetIfNeeded();
                    HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                }
            }
            else
            {
                Debug.Log("[Save System] Chưa có file save nào (Game mới). Bắt đầu với Data gốc.");
                Data = new GameData();
                ApplyEditorKeyResetIfNeeded();
                HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
            }
        }

        /// <summary>
        /// Editor only: sau khi load save, ep chìa về 0 để test scene không bị dính chìa cũ.
        /// Chỉ sửa RAM, không ghi đè cloud ngay.
        /// </summary>
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