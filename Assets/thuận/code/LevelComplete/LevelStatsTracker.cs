using System.Collections;
using System.Collections.Generic;
using HeartOfTheNight.Hung;
using HeartOfTheNight.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đếm enemies / secrets / thời gian trong màn đang chơi.
/// Enemies: chỉ slot spawn của RoomSpawnController / EnemySpawner (1 spawn point = 1 quái).
/// Không tính quái đặt sẵn trong scene, minion, summon.
/// Secret: cửa Counts As Secret — đi qua là tìm thấy (RAM). Ghi file lúc checkpoint.
/// </summary>
public class LevelStatsTracker : MonoBehaviour
{
    public static LevelStatsTracker Instance { get; private set; }

    /// <summary>Bản ghi lúc hiện Level Complete — sống qua unload scene.</summary>
    public static LevelCompleteStats LastSnapshot { get; private set; }

    private readonly HashSet<int> _trackedEnemyIds = new HashSet<int>();
    private readonly HashSet<string> _secretIds = new HashSet<string>();
    private readonly HashSet<string> _foundSecrets = new HashSet<string>();

    private int _enemiesKilled;
    private int _enemiesTotal;
    private string _sceneName = "";
    private Coroutine _censusRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static LevelStatsTracker EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<LevelStatsTracker>();
        if (existing != null)
            return existing;

        var go = new GameObject("LevelStatsTracker");
        return go.AddComponent<LevelStatsTracker>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        BeginScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BeginScene(scene.name);
    }

    private void BeginScene(string sceneName)
    {
        if (_censusRoutine != null)
            StopCoroutine(_censusRoutine);

        ResetSession();
        _sceneName = sceneName ?? "";

        if (!DataManager.IsLevelScene(_sceneName))
            return;

        _censusRoutine = StartCoroutine(CensusWhenReady());
    }

    private void ResetSession()
    {
        _trackedEnemyIds.Clear();
        _secretIds.Clear();
        _foundSecrets.Clear();
        _enemiesKilled = 0;
        _enemiesTotal = 0;
    }

    private IEnumerator CensusWhenReady()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        CensusSecrets();
        SeedFoundSecretsFromSave();
        RecountEnemiesFromWorld();
        _censusRoutine = null;
    }

    public static LevelCompleteStats CaptureSnapshot()
    {
        EnsureExists();
        var tracker = Instance;
        if (tracker != null)
            tracker.RecountEnemiesFromWorld();

        float time = DataManager.Instance != null ? DataManager.Instance.LevelTimeSeconds : 0f;

        int total = tracker != null ? Mathf.Max(0, tracker._enemiesTotal) : 0;
        int killed = tracker != null ? Mathf.Clamp(tracker._enemiesKilled, 0, total) : 0;
        int secretTotal = tracker != null ? tracker._secretIds.Count : 0;
        int secretFound = tracker != null ? tracker._foundSecrets.Count : 0;
        if (secretFound > secretTotal)
            secretTotal = secretFound;

        LastSnapshot = new LevelCompleteStats
        {
            enemiesKilled = killed,
            enemiesTotal = total,
            secretsFound = secretFound,
            secretsTotal = secretTotal,
            timeSeconds = time,
            sceneName = tracker != null ? tracker._sceneName : SceneManager.GetActiveScene().name
        };
        return LastSnapshot;
    }

    /// <summary>Quái đặt sẵn trong scene — không tính vào Level Complete.</summary>
    public static void BindExistingEnemy(GameObject enemy)
    {
        _ = enemy;
    }

    /// <summary>Enemy spawn từ wave/spawner — tổng đã cộng lúc census theo spawn point.</summary>
    public static void BindSpawnedEnemy(GameObject enemy)
    {
        EnsureExists()?.BindSpawned(enemy);
    }

    public static void NotifyEnemyKilled(GameObject enemy)
    {
        if (Instance == null || enemy == null)
            return;
        if (LevelCompleteUI.IsShowing)
            return;

        // Chỉ tính kill của quái do room/spawner đẻ ra. Tag lúc Die có thể đã Untagged.
        if (!Instance.IsTrackedEnemy(enemy))
            return;

        Instance._enemiesKilled++;
    }

    private bool IsTrackedEnemy(GameObject enemy)
    {
        int id = enemy.GetInstanceID();
        if (_trackedEnemyIds.Contains(id))
            return true;

        Transform root = enemy.transform != null ? enemy.transform.root : null;
        return root != null && _trackedEnemyIds.Contains(root.GetInstanceID());
    }

    public static void DiscoverSecret(string secretId)
    {
        if (string.IsNullOrEmpty(secretId))
            return;

        EnsureExists();
        if (Instance == null)
            return;

        Instance._secretIds.Add(secretId);
        Instance._foundSecrets.Add(secretId);

        var data = DataManager.Instance != null ? DataManager.Instance.Data : null;
        if (data == null) return;
        data.EnsureLists();
        if (!data.foundSecrets.Contains(secretId))
            data.foundSecrets.Add(secretId);
    }

    private void SeedFoundSecretsFromSave()
    {
        var data = DataManager.Instance != null ? DataManager.Instance.Data : null;
        if (data == null)
            return;

        data.EnsureLists();
        for (int i = 0; i < data.foundSecrets.Count; i++)
        {
            string id = data.foundSecrets[i];
            if (GameData.IdBelongsToScene(id, _sceneName))
                _foundSecrets.Add(id);
        }
    }

    private void BindSpawned(GameObject enemy)
    {
        if (enemy == null)
            return;

        GameObject root = enemy.transform.root.gameObject;
        if (CompareTagSafe(root, "Player") || root.GetComponent<HeartOfTheNight.Player.PlayerHealth>() != null)
            return;

        int id = root.GetInstanceID();
        _trackedEnemyIds.Add(id);
        _trackedEnemyIds.Add(enemy.GetInstanceID());

        if (root.GetComponent<EnemyKillReporter>() == null)
            root.AddComponent<EnemyKillReporter>();
        if (enemy != root && enemy.GetComponent<EnemyKillReporter>() == null)
            enemy.AddComponent<EnemyKillReporter>();
    }

    private void RecountEnemiesFromWorld()
    {
        int total = 0;
        int killed = 0;

        var rooms = FindObjectsByType<RoomSpawnController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room == null)
                continue;

            int planned = room.CountPlannedEnemies();
            if (planned <= 0)
                continue;

            total += planned;
            killed += room.CountDefeatedEnemies();
        }

        var spawners = FindObjectsByType<EnemySpawner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            var spawner = spawners[i];
            if (spawner == null)
                continue;

            int planned = spawner.CountPlannedEnemies();
            if (planned <= 0)
                continue;

            total += planned;
            killed += spawner.CountDefeatedEnemies();
        }

        _enemiesTotal = total;
        _enemiesKilled = Mathf.Clamp(killed, 0, total);
    }

    private void CensusSecrets()
    {
        var doors = FindObjectsByType<RoomTransition>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < doors.Length; i++)
        {
            var door = doors[i];
            if (door != null && door.CountsAsSecret)
                _secretIds.Add(door.SecretId);
        }
    }

    private static bool CompareTagSafe(GameObject go, string tag)
    {
        if (go == null)
            return false;
        try
        {
            return go.CompareTag(tag);
        }
        catch (UnityException)
        {
            return false;
        }
    }
}
