using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HeartOfTheNight.Rooms
{
    [RequireComponent(typeof(Collider2D))]
    public class RoomSpawnController : MonoBehaviour
    {
        //ss
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

        [Header("VFX")]
        [SerializeField] private GameObject spawnVfxPrefab;

        [Header("Spawn")]
        [SerializeField] private List<SpawnWave> waves = new();
        [Tooltip("Thời gian quái bị khựng lại (không AI, không di chuyển) ngay sau khi sinh ra.")]
        [SerializeField] private float spawnFreezeDuration = 1.5f;

        [Header("Door")]
        [SerializeField] private GameObject[] doors;
        [SerializeField] private bool closeDoorsOnStart = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onRoomStarted;
        [SerializeField] private UnityEvent onRoomCleared;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private RoomState state = RoomState.Idle;
        private readonly List<GameObject> aliveEnemies = new();
        private int currentWaveIndex = -1;
        private bool isSpawning;

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
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (state != RoomState.Idle) return;
            if (!IsPlayer(other)) return;

            StartRoom();
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag(playerTag)) return true;
            return other.transform.root.CompareTag(playerTag);
        }

        private void StartRoom()
        {
            state = RoomState.Fighting;
            currentWaveIndex = -1;

            if (closeDoorsOnStart) SetDoorsClosed(true);
            if (debugLogs) Debug.Log($"[{name}] Phong bat dau!", this);
            onRoomStarted?.Invoke();

            SpawnNextWave();
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
            aliveEnemies.Add(enemy);

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
            // Tạm tắt toàn bộ các script tự viết (logic AI, di chuyển...) để quái khựng lại
            var scripts = enemy.GetComponentsInChildren<MonoBehaviour>();
            var disabledScripts = new List<MonoBehaviour>();

            foreach (var s in scripts)
            {
                // Bỏ qua các component gốc của Unity, chỉ lấy script tự code
                if (s.enabled && (s.GetType().Namespace == null || !s.GetType().Namespace.StartsWith("UnityEngine")))
                {
                    s.enabled = false;
                    disabledScripts.Add(s);
                }
            }

            // Đóng băng vị trí vật lý (không bị trôi hay bị đẩy lùi trong lúc khựng)
            var rb = enemy.GetComponent<Rigidbody2D>();
            var oldConstraints = RigidbodyConstraints2D.None;
            if (rb != null)
            {
                oldConstraints = rb.constraints;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            yield return new WaitForSeconds(duration);

            if (enemy == null) yield break;

            // Hết thời gian: Mở khóa vật lý và bật lại toàn bộ script AI
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
            SetDoorsClosed(false);

            onRoomCleared?.Invoke();

            if (!activateOnce)
            {
                state = RoomState.Idle;
                aliveEnemies.Clear();
                currentWaveIndex = -1;
            }
        }

        private void SetDoorsClosed(bool closed)
        {
            if (doors == null) return;
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null) doors[i].SetActive(closed);
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