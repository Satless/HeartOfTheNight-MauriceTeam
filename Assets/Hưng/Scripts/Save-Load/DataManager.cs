using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
        public int playerScore;
        public string currentScene;
        public string targetSpawnID; 
        public Vector3 playerPosition; // Thêm vị trí Player
        public List<string> clearedRooms = new List<string>();

        public int maxUnlockedLevel = 1; // Mặc định luôn mở Level 1\

        // Chìa khóa cửa (Blue/Red) + cửa đã mở khóa vĩnh viễn
        public int blueKeys;
        public int redKeys;
        public bool collectedBlueKey; // true sau khi nhặt lần đầu (HUD)
        public bool collectedRedKey;
        public List<string> unlockedDoors = new List<string>();

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
            Debug.Log("[DataManager] Editor: reset chìa về 0 cho session Play này.");
#endif
        }
    }
}