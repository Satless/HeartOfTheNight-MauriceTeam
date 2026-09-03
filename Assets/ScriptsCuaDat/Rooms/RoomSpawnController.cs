using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace HeartOfTheNight.Rooms
{
    [RequireComponent(typeof(Collider2D))]
    public class RoomSpawnController : MonoBehaviour
    {
        //aa
        private enum RoomState { Idle, Fighting, Cleared }

        [System.Serializable]
        public class SpawnEntry
        {
            public GameObject enemyPrefab;
            public Transform spawnPoint;
        }

        [System.Serializable]
        public class SpawnWave
        {
            public string waveName = "Wave";
            public List<SpawnEntry> enemies = new();
            public float spawnInterval = 0f;
        }

        [Header("Trigger")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool activateOnce = true;
        [Tooltip("ID lưu phòng đã clear. Để trống = Scene + đường dẫn Hierarchy (không trùng dù trùng tên object).")]
        [SerializeField] private string roomSaveId = "";

        [Header("VFX")]
        [SerializeField] private GameObject spawnVfxPrefab;

        [Header("Spawn")]
        [SerializeField] private List<SpawnWave> waves = new();
        [Tooltip("Thời gian quái bị khựng lại (không AI, không di chuyển) ngay sau khi sinh ra.")]
        [SerializeField] private float spawnFreezeDuration = 1.5f;

        [Header("Door")]
        [SerializeField] private RoomDoor[] doors;
        [SerializeField] private bool closeDoorsOnStart = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onRoomStarted;
        [SerializeField] private UnityEvent onRoomCleared;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private static readonly Dictionary<string, RoomSpawnController> IdRegistry = new();

        private RoomState state = RoomState.Idle;
        private readonly List<GameObject> aliveEnemies = new();
        private int currentWaveIndex = -1;
        private int spawnedThisActivation;
        private bool statsCleared;
        private bool isSpawning;
        private string hierarchyKey;
        private string registeredId;
        private readonly Collider2D[] overlapBuf = new Collider2D[16];

        public bool IsCleared => state == RoomState.Cleared;

        /// <summary>
        /// Phòng không có quái (chỉ dùng làm trigger mở cửa) thì không ghi vào save:
        /// không có gì để khôi phục, mà ghi vào lại làm phòng khác tưởng mình đã clear.
        /// </summary>
        private bool HasEnemiesToSpawn => CountPlannedEnemies() > 0;

        /// <summary>
        /// Số quái sẽ đẻ ra: mỗi spawn point (kèm prefab) trong waves = 1 quái.
        /// </summary>
        public int CountPlannedEnemies()
        {
            int count = 0;
            if (waves == null)
                return 0;

            for (int w = 0; w < waves.Count; w++)
            {
                var wave = waves[w];
                if (wave?.enemies == null)
                    continue;

                for (int i = 0; i < wave.enemies.Count; i++)
                {
                    var entry = wave.enemies[i];
                    if (entry != null && entry.enemyPrefab != null)
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Số quái đã hạ trong phòng này (phòng clear = đủ planned; đang đánh = spawn rồi chết).
        /// </summary>
        public int CountDefeatedEnemies()
        {
            int planned = CountPlannedEnemies();
            if (planned <= 0)
                return 0;

            if (state == RoomState.Cleared || statsCleared)
                return planned;

            PruneDeadEnemies();
            return Mathf.Clamp(spawnedThisActivation - aliveEnemies.Count, 0, planned);
        }

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
                Debug.LogWarning($"[{name}] Collider2D nen bat 'Is Trigger'", this);

            RegisterRoomId();
            TryApplyClearedFromSave();
        }

        private IEnumerator Start()
        {
            TryApplyClearedFromSave();
            if (state != RoomState.Idle)
                yield break;

            var col = GetComponent<Collider2D>();
            if (col == null)
                yield break;

            bool wasEnabled = col.enabled;
            col.enabled = false;

            float t = 0f;
            const float timeout = 3f;
            while (t < timeout && ShouldWaitForSpawnApply())
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return null;
            yield return new WaitForFixedUpdate();

            TryApplyClearedFromSave();
            if (this == null) yield break;
            if (state == RoomState.Idle && wasEnabled)
                col.enabled = true;

            // Continue/checkpoint teleport sẵn trong trigger: OnTriggerEnter không chạy.
            yield return new WaitForFixedUpdate();
            if (this == null) yield break;
            TryStartIfPlayerAlreadyInside();
        }

        private static bool ShouldWaitForSpawnApply()
        {
            if (!string.IsNullOrEmpty(LevelEntrance.PendingSpawnID))
                return true;

            var dm = HeartOfTheNight.Hung.DataManager.Instance;
            return dm != null && dm.IsApplyingSpawnRestore;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (state != RoomState.Idle) return;
            if (!IsPlayer(other)) return;

            StartRoom();
        }

        /// <summary>
        /// Chỉ phòng có quái, chỉ 1 lần sau load. Không Stay — tránh khóa cửa khi còn đứng hành lang.
        /// </summary>
        private void TryStartIfPlayerAlreadyInside()
        {
            if (state != RoomState.Idle) return;
            if (!HasEnemiesToSpawn) return;

            var roomCol = GetComponent<Collider2D>();
            if (roomCol == null || !roomCol.enabled) return;
            if (!PlayerOverlapsRoom(roomCol)) return;

            StartRoom();
        }

        private bool PlayerOverlapsRoom(Collider2D roomCol)
        {
            var filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;
            int n = roomCol.Overlap(filter, overlapBuf);
            for (int i = 0; i < n; i++)
            {
                if (IsPlayer(overlapBuf[i]))
                    return true;
            }

            return false;
        }

        private string GetRoomId()
        {
            string scene = gameObject.scene.name;
            if (string.IsNullOrEmpty(scene))
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scene))
                scene = "UnknownScene";

            if (!string.IsNullOrEmpty(roomSaveId))
            {
                // ID gõ tay vẫn phải gắn scene, không thì phòng scene khác trùng ID theo.
                return roomSaveId.StartsWith(scene + "_", System.StringComparison.OrdinalIgnoreCase)
                    ? roomSaveId
                    : scene + "_" + roomSaveId;
            }

            return scene + "_" + GetHierarchyKey();
        }

        /// <summary>
        /// Nhiều phòng trong một scene trùng tên nhau ("Room1", "Room1 (1)") nên tên object
        /// không đủ làm ID. Ghép đường dẫn cha + thứ tự trong Hierarchy để mỗi phòng một ID.
        /// Đổi tên hoặc kéo đổi thứ tự phòng = ID mới: save cũ coi như phòng chưa clear.
        /// </summary>
        private string GetHierarchyKey()
        {
            if (hierarchyKey != null)
                return hierarchyKey;

            var sb = new StringBuilder();
            AppendHierarchyKey(transform, sb);
            hierarchyKey = sb.ToString();
            return hierarchyKey;
        }

        private static void AppendHierarchyKey(Transform t, StringBuilder sb)
        {
            if (t.parent != null)
            {
                AppendHierarchyKey(t.parent, sb);
                sb.Append('/');
            }

            sb.Append(t.name).Append('#').Append(t.GetSiblingIndex());
        }

        private void RegisterRoomId()
        {
            if (!HasEnemiesToSpawn) return;

            string id = GetRoomId();
            if (IdRegistry.TryGetValue(id, out var other) && other != null && other != this)
            {
                Debug.LogError(
                    $"[{name}] Trùng ID save phòng '{id}' với '{other.name}'. Clear một phòng sẽ xóa quái phòng kia — " +
                    "đổi tên object hoặc điền Room Save Id khác nhau.", this);
                return;
            }

            IdRegistry[id] = this;
            registeredId = id;
        }

        private void OnDestroy()
        {
            if (registeredId != null
                && IdRegistry.TryGetValue(registeredId, out var current)
                && current == this)
            {
                IdRegistry.Remove(registeredId);
            }
        }

        private bool IsClearedInSave()
        {
            var dm = HeartOfTheNight.Hung.DataManager.Instance;
            return dm != null && dm.IsRoomCleared(GetRoomId());
        }

        private void TryApplyClearedFromSave()
        {
            if (state == RoomState.Cleared) return;
            if (!HasEnemiesToSpawn) return;
            if (!IsClearedInSave()) return;
            ApplyClearedFromSave();
        }

        private void ApplyClearedFromSave()
        {
            state = RoomState.Cleared;
            statsCleared = true;
            SetDoorsClosed(false);
            if (debugLogs) Debug.Log($"[{name}] Phong da clear trong save ({GetRoomId()}) — khong spawn lai.", this);
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag(playerTag)) return true;
            return other.transform.root.CompareTag(playerTag);
        }

        private void StartRoom()
        {
            // Phòng không wave: chỉ chạm collider để cửa chạy Open. Không lock-in, không ghi save.
            if (!HasEnemiesToSpawn)
            {
                PlayDoorsOnly();
                return;
            }

            state = RoomState.Fighting;
            currentWaveIndex = -1;
            spawnedThisActivation = 0;
            statsCleared = false;

            if (closeDoorsOnStart) SetDoorsClosed(true);
            if (debugLogs) Debug.Log($"[{name}] Phong bat dau!", this);
            onRoomStarted?.Invoke();

            SpawnNextWave();
        }

        private void PlayDoorsOnly()
        {
            SetDoorsClosed(false);
            onRoomStarted?.Invoke();
            onRoomCleared?.Invoke();

            if (activateOnce)
                state = RoomState.Cleared;

            if (debugLogs) Debug.Log($"[{name}] Phong khong quai — chi chay cua, khong ghi save.", this);
        }

        private void Update()
        {
            if (state != RoomState.Fighting || isSpawning) return;

            PruneDeadEnemies();

            if (aliveEnemies.Count > 0) return;

            if (currentWaveIndex + 1 < waves.Count)
                SpawnNextWave();
            else
                ClearRoom();
        }

        private void PruneDeadEnemies()
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                if (aliveEnemies[i] == null) aliveEnemies.RemoveAt(i);
            }
        }

        private void SpawnNextWave()
        {
            currentWaveIndex++;
            if (currentWaveIndex >= waves.Count)
            {
                ClearRoom();
                return;
            }

            StartCoroutine(SpawnWaveRoutine(waves[currentWaveIndex]));
        }

        private IEnumerator SpawnWaveRoutine(SpawnWave wave)
        {
            isSpawning = true;
            aliveEnemies.Clear();

            for (int i = 0; i < wave.enemies.Count; i++)
            {
                SpawnOne(wave.enemies[i]);
                if (wave.spawnInterval > 0f && i < wave.enemies.Count - 1)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }

            isSpawning = false;
        }

        private void SpawnOne(SpawnEntry entry)
        {
            if (entry == null || entry.enemyPrefab == null) return;

            Vector3 pos = entry.spawnPoint != null ? entry.spawnPoint.position : transform.position;
            Quaternion rot = entry.spawnPoint != null ? entry.spawnPoint.rotation : Quaternion.identity;

            GameObject enemy = Instantiate(entry.enemyPrefab, pos, rot);
            enemy.SetActive(true); // Thêm dòng này
            aliveEnemies.Add(enemy);
            spawnedThisActivation++;
            LevelStatsTracker.BindSpawnedEnemy(enemy);

            if (spawnVfxPrefab != null)
            {
                Vector3 vfxPos = pos;
                Collider2D col = enemy.GetComponent<Collider2D>();
                if (col != null) vfxPos = col.bounds.center;

                Instantiate(spawnVfxPrefab, vfxPos, Quaternion.identity);
            }

            // Gọi logic đóng băng ngay khi sinh ra
            if (spawnFreezeDuration > 0f)
            {
                StartCoroutine(ApplySpawnFreeze(enemy, spawnFreezeDuration));
            }
        }

        private IEnumerator ApplySpawnFreeze(GameObject enemy, float duration)
        {
            if (enemy == null) yield break;

            // Cho Awake/Start chạy xong (gán currentHealth = maxHealth) rồi mới freeze.
            // Nếu disable ngay frame spawn, health bar đọc HP = 0 và tưởng quái đã chết.
            yield return null;
            if (enemy == null) yield break;

            // Boss: khong disable script (tranh restart AttackLoop). Chi bao delay dung yen.
            var boss = enemy.GetComponent<HeartOfTheNight.Enemy.HeartOfTheNightBoss>();
            if (boss != null)
            {
                boss.ApplySpawnHold(duration);
                yield return new WaitForSeconds(duration);
                yield break;
            }

            // Quai thuong: tạm tắt script AI / di chuyển
            var scripts = enemy.GetComponentsInChildren<MonoBehaviour>();
            var disabledScripts = new List<MonoBehaviour>();

            foreach (var s in scripts)
            {
                if (s == null || !s.enabled) continue;
                if (s is EnemyHealthBar) continue;
                if (s.GetType().Namespace == null || !s.GetType().Namespace.StartsWith("UnityEngine"))
                {
                    s.enabled = false;
                    disabledScripts.Add(s);
                }
            }

            var rb = enemy.GetComponent<Rigidbody2D>();
            var oldConstraints = RigidbodyConstraints2D.None;
            if (rb != null)
            {
                oldConstraints = rb.constraints;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            yield return new WaitForSeconds(duration);

            if (enemy == null) yield break;

            if (rb != null) rb.constraints = oldConstraints;
            foreach (var s in disabledScripts)
            {
                if (s != null) s.enabled = true;
            }
        }

        private void ClearRoom()
        {
            if (state == RoomState.Cleared) return;

            state = RoomState.Cleared;
            statsCleared = true;
            SetDoorsClosed(false);

            onRoomCleared?.Invoke();

            var dm = HeartOfTheNight.Hung.DataManager.Instance;
            if (dm != null && HasEnemiesToSpawn)
                dm.MarkRoomCleared(GetRoomId());

            if (!activateOnce)
            {
                state = RoomState.Idle;
                aliveEnemies.Clear();
                currentWaveIndex = -1;
            }
        }
        // Sửa lại logic đóng/mở toàn bộ cửa trong phòng:
        private void SetDoorsClosed(bool isClosed)
        {
            if (doors == null) return;
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                {
                    if (isClosed)
                        doors[i].Close();
                    else
                        doors[i].Open();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            for (int w = 0; w < waves.Count; w++)
            {
                var wave = waves[w];
                if (wave?.enemies == null) continue;
                foreach (var e in wave.enemies)
                {
                    if (e?.spawnPoint == null) continue;
                    Gizmos.DrawWireSphere(e.spawnPoint.position, 0.4f);
                    Gizmos.DrawLine(transform.position, e.spawnPoint.position);
                }
            }

            Gizmos.color = Color.cyan;
            if (doors != null)
            {
                foreach (var d in doors)
                {
                    if (d == null) continue;
                    Gizmos.DrawWireCube(d.transform.position, Vector3.one * 0.6f);
                    Gizmos.DrawLine(transform.position, d.transform.position);
                }
            }
        }
    }
}