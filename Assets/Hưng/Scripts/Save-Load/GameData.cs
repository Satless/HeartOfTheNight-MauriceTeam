using System;
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

        /// <summary>
        /// Ô súng 1–4. Ô 1 luôn mở. Slot-wide: không xóa khi PrepareForNewScene / chết.
        /// </summary>
        public bool[] unlockedWeapons = new bool[] { true, false, false, false };

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

        /// <summary>
        /// Đồng hồ màn lúc qua cửa. Home / ghi file khi thoát app rollback về đây.
        /// Chết / Continue không restore — giữ giây đang treo.
        /// false = save cũ, chưa từng snapshot timer.
        /// </summary>
        public bool hasCheckpointScenePlayTimes;
        public List<ScenePlayTimeEntry> checkpointScenePlayTimes = new List<ScenePlayTimeEntry>();

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
            if (checkpointScenePlayTimes == null) checkpointScenePlayTimes = new List<ScenePlayTimeEntry>();
            EnsureUnlockedWeapons();
            Sanitize();
        }

        /// <summary>
        /// Chặn save sửa tay / cloud lạ: máu, chìa, list ID. Không thay gameplay bình thường.
        /// </summary>
        public void Sanitize()
        {
            slotIndex = Mathf.Clamp(slotIndex, 1, 4);
            playerHealth = Mathf.Clamp(playerHealth, 0, 999);
            checkpointPlayerHealth = Mathf.Clamp(checkpointPlayerHealth, 0, 999);
            playerCoin = Mathf.Clamp(playerCoin, 0, 999999);
            blueKeys = Mathf.Clamp(blueKeys, 0, 99);
            redKeys = Mathf.Clamp(redKeys, 0, 99);
            checkpointBlueKeys = Mathf.Clamp(checkpointBlueKeys, 0, 99);
            checkpointRedKeys = Mathf.Clamp(checkpointRedKeys, 0, 99);

            int levelCap = 32;
            int total = ChapterProgress.TotalSceneCount;
            if (total > 0)
                levelCap = total;
            maxUnlockedLevel = Mathf.Clamp(maxUnlockedLevel, 1, levelCap);

            totalPlayTimeSeconds = SanitizeTime(totalPlayTimeSeconds);
            currentScene = ClampText(currentScene, 128);
            targetSpawnID = ClampText(targetSpawnID, 160);
            checkpointScene = ClampText(checkpointScene, 128);
            checkpointSpawnID = ClampText(checkpointSpawnID, 160);
            createdAtUtc = ClampText(createdAtUtc, 64);
            lastPlayedAtUtc = ClampText(lastPlayedAtUtc, 64);
            playerPosition = SanitizePos(playerPosition);
            checkpointPosition = SanitizePos(checkpointPosition);

            SanitizeIdList(clearedRooms);
            SanitizeIdList(unlockedDoors);
            SanitizeIdList(collectedKeyPickupIds);
            SanitizeIdList(foundSecrets);
            SanitizeIdList(checkpointClearedRooms);
            SanitizeIdList(checkpointUnlockedDoors);
            SanitizeIdList(checkpointCollectedKeyPickupIds);
            SanitizeIdList(checkpointFoundSecrets);
            SanitizeSceneTimers(scenePlayTimes);
            SanitizeSceneTimers(checkpointScenePlayTimes);
        }

        private static float SanitizeTime(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                return 0f;
            const float maxSeconds = 86400f * 365f * 10f;
            return seconds > maxSeconds ? maxSeconds : seconds;
        }

        private static Vector3 SanitizePos(Vector3 pos)
        {
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) ||
                float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z))
                return Vector3.zero;
            return new Vector3(
                Mathf.Clamp(pos.x, -100000f, 100000f),
                Mathf.Clamp(pos.y, -100000f, 100000f),
                Mathf.Clamp(pos.z, -100000f, 100000f));
        }

        private static string ClampText(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (value.IndexOf('\0') >= 0)
                return "";
            return value.Length <= maxLen ? value : value.Substring(0, maxLen);
        }

        private static void SanitizeIdList(List<string> list)
        {
            if (list == null)
                return;

            const int maxItems = 2048;
            const int maxLen = 160;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                string id = list[i];
                if (string.IsNullOrEmpty(id) || id.Length > maxLen || id.IndexOf('\0') >= 0)
                    list.RemoveAt(i);
            }

            while (list.Count > maxItems)
                list.RemoveAt(list.Count - 1);
        }

        private static void SanitizeSceneTimers(List<ScenePlayTimeEntry> list)
        {
            if (list == null)
                return;

            const int maxItems = 64;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                ScenePlayTimeEntry entry = list[i];
                if (entry == null || string.IsNullOrEmpty(entry.sceneName))
                {
                    list.RemoveAt(i);
                    continue;
                }

                entry.sceneName = ClampText(entry.sceneName, 128);
                entry.playSeconds = SanitizeTime(entry.playSeconds);
            }

            while (list.Count > maxItems)
                list.RemoveAt(list.Count - 1);
        }

        public void EnsureUnlockedWeapons()
        {
            const int slotCount = 4;
            if (unlockedWeapons == null || unlockedWeapons.Length != slotCount)
            {
                bool[] next = new bool[slotCount];
                next[0] = true;
                if (unlockedWeapons != null)
                {
                    int copy = unlockedWeapons.Length < slotCount ? unlockedWeapons.Length : slotCount;
                    for (int i = 0; i < copy; i++)
                        next[i] = unlockedWeapons[i];
                }
                unlockedWeapons = next;
            }

            unlockedWeapons[0] = true;
        }

        public bool IsWeaponUnlocked(int slotIndex)
        {
            EnsureUnlockedWeapons();
            if (slotIndex < 1 || slotIndex > unlockedWeapons.Length)
                return false;
            return unlockedWeapons[slotIndex - 1];
        }

        /// <summary>Ghi RAM slot. Không đụng chìa/phòng. File ghi lúc checkpoint / rời màn.</summary>
        public bool UnlockWeapon(int slotIndex)
        {
            EnsureUnlockedWeapons();
            if (slotIndex < 1 || slotIndex > unlockedWeapons.Length)
                return false;
            if (unlockedWeapons[slotIndex - 1])
                return false;

            unlockedWeapons[slotIndex - 1] = true;
            return true;
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
            hasCheckpointScenePlayTimes = true;
            checkpointScenePlayTimes = CloneScenePlayTimes(scenePlayTimes);
        }

        /// <summary>
        /// RAM đang chơi (chìa/phòng chưa commit). Dùng khi ghi file mà không được đụng HUD.
        /// </summary>
        public WorldLiveState CopyLiveWorld()
        {
            EnsureLists();
            return new WorldLiveState
            {
                clearedRooms = new List<string>(clearedRooms),
                unlockedDoors = new List<string>(unlockedDoors),
                collectedKeyPickupIds = new List<string>(collectedKeyPickupIds),
                foundSecrets = new List<string>(foundSecrets),
                blueKeys = blueKeys,
                redKeys = redKeys,
                collectedBlueKey = collectedBlueKey,
                collectedRedKey = collectedRedKey,
                playerHealth = playerHealth,
            };
        }

        public void ApplyLiveWorld(WorldLiveState live)
        {
            if (live == null)
                return;

            clearedRooms = live.clearedRooms ?? new List<string>();
            unlockedDoors = live.unlockedDoors ?? new List<string>();
            collectedKeyPickupIds = live.collectedKeyPickupIds ?? new List<string>();
            foundSecrets = live.foundSecrets ?? new List<string>();
            blueKeys = live.blueKeys;
            redKeys = live.redKeys;
            collectedBlueKey = live.collectedBlueKey;
            collectedRedKey = live.collectedRedKey;
            playerHealth = live.playerHealth;
        }

        /// <summary>
        /// Chết / Home: phòng/chìa/cửa sau checkpoint trở lại như lúc qua cửa (hoặc lúc vào màn).
        /// Không có snapshot thì giữ nguyên — không xóa clearedRooms cả slot.
        /// </summary>
        public void RestoreCheckpointWorldState()
        {
            EnsureLists();
            if (!hasCheckpointWorldState)
                return;

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
        /// Home / ghi committed xuống đĩa. Không gọi lúc chết — đồng hồ vẫn chạy tiếp từ chỗ treo.
        /// </summary>
        public void RestoreCheckpointScenePlayTimes()
        {
            EnsureLists();
            if (!hasCheckpointWorldState || !hasCheckpointScenePlayTimes)
                return;

            scenePlayTimes = CloneScenePlayTimes(checkpointScenePlayTimes);
        }

        public List<ScenePlayTimeEntry> CopyLiveScenePlayTimes()
        {
            EnsureLists();
            return CloneScenePlayTimes(scenePlayTimes);
        }

        public void ApplyLiveScenePlayTimes(List<ScenePlayTimeEntry> live)
        {
            scenePlayTimes = CloneScenePlayTimes(live);
        }

        private static List<ScenePlayTimeEntry> CloneScenePlayTimes(List<ScenePlayTimeEntry> source)
        {
            var result = new List<ScenePlayTimeEntry>();
            if (source == null)
                return result;

            for (int i = 0; i < source.Count; i++)
            {
                ScenePlayTimeEntry entry = source[i];
                if (entry == null || string.IsNullOrEmpty(entry.sceneName))
                    continue;
                result.Add(new ScenePlayTimeEntry
                {
                    sceneName = entry.sceneName,
                    playSeconds = entry.playSeconds
                });
            }

            return result;
        }

        /// <summary>
        /// ID mặc định: SceneName_ObjectName. Dùng khi replay / vào màn từ Select Level.
        /// </summary>
        public static bool IdBelongsToScene(string id, string sceneName)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(sceneName))
                return false;
            return id.StartsWith(sceneName + "_", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Xóa phòng/chìa/cửa/secret/timer của đúng scene — chơi lại từ đầu (speedrun).
        /// Scene khác trong slot không đụng.
        /// </summary>
        public void ClearSceneLocalProgress(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            EnsureLists();
            RemoveIdsForScene(clearedRooms, sceneName);
            RemoveIdsForScene(unlockedDoors, sceneName);
            RemoveIdsForScene(collectedKeyPickupIds, sceneName);
            RemoveIdsForScene(foundSecrets, sceneName);

            ScenePlayTimeEntry timer = FindScenePlayTime(sceneName);
            if (timer != null)
                timer.playSeconds = 0f;
        }

        private static void RemoveIdsForScene(List<string> list, string sceneName)
        {
            if (list == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (IdBelongsToScene(list[i], sceneName))
                    list.RemoveAt(i);
            }
        }

        /// <summary>
        /// Bỏ màn đang dở: world về snapshot checkpoint gần nhất, rồi hết in-progress.
        /// Không xóa phòng đã clear ở checkpoint.
        /// </summary>
        public void ClearInProgressWorldState()
        {
            EnsureLists();
            if (hasCheckpointWorldState)
            {
                RestoreCheckpointWorldState();
                RestoreCheckpointScenePlayTimes();
            }

            hasCheckpoint = false;
            checkpointScene = "";
            checkpointSpawnID = "";
            checkpointPosition = Vector3.zero;
            CaptureCheckpointWorldState();
        }

        /// <summary>Qua màn mà cửa không phải checkpoint — hết điểm hồi, giữ snapshot world đã commit.</summary>
        public void ClearCheckpointFlags()
        {
            hasCheckpoint = false;
            checkpointScene = "";
            checkpointSpawnID = "";
            checkpointPosition = Vector3.zero;
        }

        /// <summary>Bản copy RAM world — không ghi vào save JSON.</summary>
        public class WorldLiveState
        {
            public List<string> clearedRooms;
            public List<string> unlockedDoors;
            public List<string> collectedKeyPickupIds;
            public List<string> foundSecrets;
            public int blueKeys;
            public int redKeys;
            public bool collectedBlueKey;
            public bool collectedRedKey;
            public int playerHealth;
        }
    }
}
