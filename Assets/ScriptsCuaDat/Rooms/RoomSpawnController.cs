using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HeartOfTheNight.Rooms
{
    
    //zz
    //xx
    //zz
    //cc
    //xx
    //cccc
    //cc
    //cc
    //cc
    //cc
    //cc
    //hihi
    //dd

    /// <summary>
    /// Co che phong dau:
    /// 1. Player buoc vao vung trigger -> sinh quai tai cac diem dat san.
    /// 2. (Tuy chon) Dong cua lai de nhot Player trong phong.
    /// 3. Khi tat ca quai trong phong da chet -> mo cua + ban event onRoomCleared.

    /// Gan script nay vao 1 GameObject co Collider2D (isTrigger = true) bao trum vung kich hoat.
    /// Khong can sua cac script quai: quai chet goi Destroy(gameObject), tham chieu Unity tro thanh null,
    /// nen ta chi can dem so quai con song.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class RoomSpawnController : MonoBehaviour
    {
        private enum RoomState { Idle, Fighting, Cleared }

        [System.Serializable]
        public class SpawnEntry
        {
            [Tooltip("Prefab quai se duoc sinh ra.")]
            public GameObject enemyPrefab;

            [Tooltip("Vi tri dat san de sinh quai. De trong = sinh tai vi tri cua RoomSpawnController.")]
            public Transform spawnPoint;
        }

        [System.Serializable]
        public class SpawnWave
        {
            [Tooltip("Ten dot (chi de de doc trong Inspector).")]
            public string waveName = "Wave";

            [Tooltip("Danh sach quai sinh ra trong dot nay.")]
            public List<SpawnEntry> enemies = new();

            [Tooltip("Thoi gian cho giua moi lan sinh quai trong cung 1 dot (giay). 0 = sinh dong loat.")]
            public float spawnInterval = 0f;
        }

        [Header("Trigger")]
        [Tooltip("Tag cua Player de nhan dien khi vao vung trigger.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Chi kich hoat phong 1 lan duy nhat (khong reset khi Player ra/vao lai).")]
        [SerializeField] private bool activateOnce = true;

        [Header("Spawn")]
        [Tooltip("Cac dot quai. Dot sau chi sinh khi dot truoc da bi tieu diet het. Chi can 1 dot neu muon don gian.")]
        [SerializeField] private List<SpawnWave> waves = new();

        [Header("Door")]
        [Tooltip("Cac cua/vat can. Khi phong bat dau se duoc BAT (dong cua); khi sach quai se duoc TAT (mo cua).")]
        [SerializeField] private GameObject[] doors;

        [Tooltip("Dong cua ngay khi phong bat dau (nhot Player lai).")]
        [SerializeField] private bool closeDoorsOnStart = true;

        [Header("Events")]
        [Tooltip("Goi khi Player kich hoat phong (vua bat dau danh).")]
        [SerializeField] private UnityEvent onRoomStarted;

        [Tooltip("Goi khi tat ca quai trong phong da chet (cua mo).")]
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
                Debug.LogWarning($"[{name}] Collider2D nen bat 'Is Trigger' de phat hien Player vao phong.", this);

            if (waves.Count == 0)
                Debug.LogWarning($"[{name}] Chua cau hinh dot quai nao (Waves rong) - phong se mo cua ngay khi kich hoat.", this);
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

            if (debugLogs) Debug.Log($"[{name}] Phong bat dau! Player da vao trigger.", this);
            onRoomStarted?.Invoke();

            SpawnNextWave();
        }

        private void Update()
        {
            if (state != RoomState.Fighting || isSpawning) return;

            PruneDeadEnemies();

            if (aliveEnemies.Count > 0) return;

            // Het quai cua dot hien tai -> sang dot tiep, hoac don phong neu het dot.
            if (currentWaveIndex + 1 < waves.Count)
                SpawnNextWave();
            else
                ClearRoom();
        }

        private void PruneDeadEnemies()
        {
            for (int i = aliveEnemies.Count - 1; i >= 0; i--)
            {
                // Unity ghi de == null cho object da Destroy.
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

            if (debugLogs)
                Debug.Log($"[{name}] Sinh dot {currentWaveIndex + 1}/{waves.Count} ('{wave.waveName}') - {wave.enemies.Count} quai.", this);

            for (int i = 0; i < wave.enemies.Count; i++)
            {
                SpawnOne(wave.enemies[i]);
                if (wave.spawnInterval > 0f && i < wave.enemies.Count - 1)
                    yield return new WaitForSeconds(wave.spawnInterval);
            }

            isSpawning = false;

            // Truong hop dot rong: cho Update xu ly chuyen dot/mo cua o frame sau.
        }

        private void SpawnOne(SpawnEntry entry)
        {
            if (entry == null || entry.enemyPrefab == null)
            {
                Debug.LogWarning($"[{name}] Co 1 SpawnEntry thieu prefab - bo qua.", this);
                return;
            }

            Vector3 pos = entry.spawnPoint != null ? entry.spawnPoint.position : transform.position;
            Quaternion rot = entry.spawnPoint != null ? entry.spawnPoint.rotation : Quaternion.identity;

            GameObject enemy = Instantiate(entry.enemyPrefab, pos, rot);
            aliveEnemies.Add(enemy);
        }

        private void ClearRoom()
        {
            if (state == RoomState.Cleared) return;

            state = RoomState.Cleared;
            SetDoorsClosed(false);

            if (debugLogs) Debug.Log($"[{name}] Da don sach phong! Mo cua.", this);
            onRoomCleared?.Invoke();

            if (!activateOnce)
            {
                // Cho phep kich hoat lai lan sau.
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
            // Ve cac diem spawn de de bo tri trong Scene.
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

            // Ve lien ket toi cac cua.
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
