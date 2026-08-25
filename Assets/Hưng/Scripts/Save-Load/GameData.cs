using System.Collections.Generic;
using UnityEngine;

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

        public float totalPlayTimeSeconds;
        public List<ScenePlayTimeEntry> scenePlayTimes = new List<ScenePlayTimeEntry>();

        /// <summary>
        /// Snapshot lúc qua cửa checkpoint. Chết rollback về đây (phòng/chìa/cửa sau cửa chưa commit).
        /// </summary>
        public bool hasCheckpointWorldState;
        public List<string> checkpointClearedRooms = new List<string>();
        public List<string> checkpointUnlockedDoors = new List<string>();
        public List<string> checkpointCollectedKeyPickupIds = new List<string>();
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
            if (scenePlayTimes == null) scenePlayTimes = new List<ScenePlayTimeEntry>();
            if (checkpointClearedRooms == null) checkpointClearedRooms = new List<string>();
            if (checkpointUnlockedDoors == null) checkpointUnlockedDoors = new List<string>();
            if (checkpointCollectedKeyPickupIds == null) checkpointCollectedKeyPickupIds = new List<string>();
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

        public void CaptureCheckpointWorldState()
        {
            EnsureLists();
            hasCheckpointWorldState = true;
            checkpointClearedRooms = new List<string>(clearedRooms);
            checkpointUnlockedDoors = new List<string>(unlockedDoors);
            checkpointCollectedKeyPickupIds = new List<string>(collectedKeyPickupIds);
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
                return;
            }

            clearedRooms = new List<string>(checkpointClearedRooms);
            unlockedDoors = new List<string>(checkpointUnlockedDoors);
            collectedKeyPickupIds = new List<string>(checkpointCollectedKeyPickupIds);
            blueKeys = checkpointBlueKeys;
            redKeys = checkpointRedKeys;
            collectedBlueKey = checkpointCollectedBlueKey;
            collectedRedKey = checkpointCollectedRedKey;
            if (checkpointPlayerHealth > 0)
                playerHealth = checkpointPlayerHealth;
        }

        public void ClearInProgressWorldState()
        {
            EnsureLists();
            hasCheckpoint = false;
            hasCheckpointWorldState = false;
            checkpointScene = "";
            checkpointSpawnID = "";
            checkpointPosition = Vector3.zero;
            clearedRooms = new List<string>();
            checkpointClearedRooms = new List<string>();
            checkpointUnlockedDoors = new List<string>();
            checkpointCollectedKeyPickupIds = new List<string>();
        }
    }
}
