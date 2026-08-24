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
    }
}
