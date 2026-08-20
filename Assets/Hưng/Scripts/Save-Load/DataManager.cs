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
    public class GameData
    {
        public int playerHealth;
        public int playerCoin;
        public string currentScene;
        public string targetSpawnID; 
        public Vector3 playerPosition; // Thêm vị trí Player
        public List<string> clearedRooms = new List<string>();

        // Checkpoint gắn cửa: chết / Continue về cửa đã kích hoạt gần nhất
        public bool hasCheckpoint;
        public string checkpointScene;
        public string checkpointSpawnID;
        public Vector3 checkpointPosition;

        public int maxUnlockedLevel = 1; // Mặc định luôn mở Level 1\

        // Chìa khóa cửa (Blue/Red) + cửa đã mở khóa vĩnh viễn
        public int blueKeys;
        public int redKeys;
        public bool collectedBlueKey; // true sau khi nhặt lần đầu (HUD)
        public bool collectedRedKey;
        public List<string> unlockedDoors = new List<string>();
        /// <summary>Id từng KeyPickup đã nhặt trên map (nhiều key cùng màu / cùng scene).</summary>
        public List<string> collectedKeyPickupIds = new List<string>();

        //sau này thêm các dữ liệu tiếp theo...
    }

    // Class quản lý sống xuyên Scene
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        public GameData Data = new GameData();

        [Header("Debug / Test")]
        [Tooltip("Chi Editor: bat = giu chìa từ save khi Play. Tat (mac dinh) = moi lan Play chìa ve 0.")]
        [SerializeField] private bool keepSavedKeysWhenPlayInEditor = false;

        [Header("Checkpoint")]
        [Tooltip("Cho anim chết chạy trước khi fade + load lại scene.")]
        [SerializeField] private float respawnDelay = 1.2f;

        private bool _pendingRespawnApply;
        private bool _isRespawning;

        private string SavePath => Application.persistentDataPath + "/save_data.json";
        private string BackupPath => Application.persistentDataPath + "/save_data.bak";
        private string TempPath => Application.persistentDataPath + "/save_data.tmp";

        private FirebaseAuth _auth;
        private FirebaseUser _user;
        private DatabaseReference _dbRef;
        private bool _isFirebaseReady = false;
        private bool _isFirebaseInitializing = false; // Thêm cờ để biết đang khởi tạo

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Khởi tạo Firebase thay vì LoadGame Local ngay lập tức
                InitializeFirebase();
            }
            else
            {
                Destroy(gameObject);
            }
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
            // Luôn lưu local làm lốp dự phòng
            SaveGameLocal();

            // Nếu Firebase đã kết nối, đẩy 1 bản copy lên Cloud
            if (_isFirebaseReady && _user != null && _dbRef != null)
            {
                string json = JsonUtility.ToJson(Data, true);
                _dbRef.Child("users").Child(_user.UserId).Child("GameData").SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        Debug.LogError("[Firebase] Lỗi khi lưu lên Cloud.");
                    else
                        Debug.Log("[Firebase] Đã đồng bộ Save lên Cloud thành công!");
                });
            }
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
            if (hp != null) hp.HealToFull();
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
            Debug.Log("[Firebase] Đang tải dữ liệu từ Cloud...");
            _dbRef.Child("users").Child(_user.UserId).Child("GameData").GetValueAsync().ContinueWithOnMainThread(task =>
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
                        
                        Debug.Log($"[Firebase] Tải Cloud thành công. Kiểm tra RAM: playerHealth={Data.playerHealth}");
                        
                        // Tải cloud xong thì lưu đè xuống Local để làm backup cho lần sau mất mạng
                        SaveGameLocal();
                        ApplyEditorKeyResetIfNeeded();
                        HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
                    }
                    else
                    {
                        Debug.Log("[Firebase] Người chơi mới! Chưa có dữ liệu trên Cloud. Đang khởi tạo data mặc định...");
                        LoadGameLocal(); // Lỡ họ có data local nhưng chưa từng lên cloud
                    }
                }
                
                // Báo cho TestSaveLoad biết là đã load xong (có thể dịch chuyển nhân vật)
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
                    Debug.Log($"[Save System] Tải local thành công. RAM: playerHealth={Data.playerHealth}");
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