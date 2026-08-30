using System.Collections.Generic;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    [System.Serializable]
    public class ScenePlayTimeEntry
    {
        public string sceneName;
        /// <summary>Số giây đã chơi trong màn này, không tính lúc nằm chết chờ hồi sinh.</summary>
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
        public Vector3 playerPosition;
        public List<string> clearedRooms = new List<string>();

        public bool hasCheckpoint;
        public string checkpointScene;
        public string checkpointSpawnID;
        public Vector3 checkpointPosition;

        public int maxUnlockedLevel = 1;

        public int blueKeys;
        public int redKeys;
        public bool collectedBlueKey;
        public bool collectedRedKey;
        public List<string> unlockedDoors = new List<string>();
        public List<string> collectedKeyPickupIds = new List<string>();
        public List<string> foundSecrets = new List<string>();

        public float totalPlayTimeSeconds;
        public List<ScenePlayTimeEntry> scenePlayTimes = new List<ScenePlayTimeEntry>();

        /// <summary>
        /// Snapshot lúc qua cửa checkpoint. Chết / Continue / thoát màn rollback về đây.
        /// File chỉ ghi tại checkpoint (và khi rời màn về menu).
        /// </summary>
        public bool hasCheckpointWorldState;
        public List<string> checkpointClearedRooms = new List<string>();
        public List<string> checkpointUnlockedDoors = new List<string>();
        public List<string> checkpointCollectedKeyPickupIds = new List<string>();
        public List<string> checkpointFoundSecrets = new List<string>();
        public int checkpointBlueKeys;
        public int checkpointRedKeys;
        public bool checkpointCollectedBlueKey;
        public bool checkpointCollectedRedKey;
        public int checkpointPlayerHealth;

        public void EnsureLists()
        {
            if (clearedRooms == null) clearedRooms = new List<string>();
            if (unlockedDoors == null) unlockedDoors = new List<string>();
            if (collectedKeyPickupIds == null) collectedKeyPickupIds = new List<string>();
            if (foundSecrets == null) foundSecrets = new List<string>();
            if (scenePlayTimes == null) scenePlayTimes = new List<ScenePlayTimeEntry>();
            if (checkpointClearedRooms == null) checkpointClearedRooms = new List<string>();
            if (checkpointUnlockedDoors == null) checkpointUnlockedDoors = new List<string>();
            if (checkpointCollectedKeyPickupIds == null) checkpointCollectedKeyPickupIds = new List<string>();
            if (checkpointFoundSecrets == null) checkpointFoundSecrets = new List<string>();
        }

        public bool IsRoomCleared(string roomId)
        {
            EnsureLists();
            return !string.IsNullOrEmpty(roomId) && clearedRooms.Contains(roomId);
        }

        public void MarkRoomCleared(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return;
            EnsureLists();
            if (!clearedRooms.Contains(roomId))
                clearedRooms.Add(roomId);
        }

        /// <summary>
        /// DataManager giữ luôn reference trả về để cộng giây mỗi frame mà không phải quét lại list.
        /// </summary>
        public ScenePlayTimeEntry GetOrCreateScenePlayTime(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;

            ScenePlayTimeEntry entry = FindScenePlayTime(sceneName);
            if (entry != null) return entry;

            entry = new ScenePlayTimeEntry { sceneName = sceneName, playSeconds = 0f };
            scenePlayTimes.Add(entry);
            return entry;
        }

        private ScenePlayTimeEntry FindScenePlayTime(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;

            EnsureLists();
            for (int i = 0; i < scenePlayTimes.Count; i++)
            {
                if (scenePlayTimes[i] != null && scenePlayTimes[i].sceneName == sceneName)
                    return scenePlayTimes[i];
            }
            return null;
        }

        public void CaptureCheckpointWorldState()
        {
            EnsureLists();
            hasCheckpointWorldState = true;
            checkpointClearedRooms = new List<string>(clearedRooms);
            checkpointUnlockedDoors = new List<string>(unlockedDoors);
            checkpointCollectedKeyPickupIds = new List<string>(collectedKeyPickupIds);
            checkpointFoundSecrets = new List<string>(foundSecrets);
            checkpointBlueKeys = blueKeys;
            checkpointRedKeys = redKeys;
            checkpointCollectedBlueKey = collectedBlueKey;
            checkpointCollectedRedKey = collectedRedKey;
            checkpointPlayerHealth = playerHealth;
        }

        /// <summary>
        /// Chết: phòng/chìa/cửa sau checkpoint trở lại như lúc qua cửa.
        /// Save cũ chưa có snapshot thì coi như chưa clear phòng nào.
        /// </summary>
        public void RestoreCheckpointWorldState()
        {
            EnsureLists();
            if (!hasCheckpointWorldState)
            {
                clearedRooms = new List<string>();
                foundSecrets = new List<string>();
                return;
            }

            clearedRooms = new List<string>(checkpointClearedRooms);
            unlockedDoors = new List<string>(checkpointUnlockedDoors);
            collectedKeyPickupIds = new List<string>(checkpointCollectedKeyPickupIds);
            foundSecrets = new List<string>(checkpointFoundSecrets);
            blueKeys = checkpointBlueKeys;
            redKeys = checkpointRedKeys;
            collectedBlueKey = checkpointCollectedBlueKey;
            collectedRedKey = checkpointCollectedRedKey;
            if (checkpointPlayerHealth > 0)
                playerHealth = checkpointPlayerHealth;
        }

        /// <summary>
        /// Bỏ màn đang dở: world về snapshot checkpoint gần nhất, rồi hết in-progress.
        /// Không xóa phòng đã clear ở checkpoint.
        /// </summary>
        public void ClearInProgressWorldState()
        {
            EnsureLists();
            if (hasCheckpointWorldState)
                RestoreCheckpointWorldState();

            hasCheckpoint = false;
            hasCheckpointWorldState = false;
            checkpointScene = "";
            checkpointSpawnID = "";
            checkpointPosition = Vector3.zero;
            checkpointClearedRooms = new List<string>();
            checkpointUnlockedDoors = new List<string>();
            checkpointCollectedKeyPickupIds = new List<string>();
            checkpointFoundSecrets = new List<string>();
        }

        /// <summary>Qua màn mà cửa không phải checkpoint — đừng giữ checkpoint màn cũ.</summary>
        public void ClearCheckpointFlags()
        {
            hasCheckpoint = false;
            hasCheckpointWorldState = false;
            checkpointScene = "";
            checkpointSpawnID = "";
            checkpointPosition = Vector3.zero;
        }
    }
}
