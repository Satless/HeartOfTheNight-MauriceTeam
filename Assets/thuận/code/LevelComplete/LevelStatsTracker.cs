using System.Collections;
using System.Collections.Generic;
using HeartOfTheNight.Hung;
using HeartOfTheNight.Rooms;
using HeartOfTheNight.ThuNghiem;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đếm enemies / secrets / thời gian trong màn đang chơi.
/// Census lúc load scene; kill khi EnemyKillReporter Destroy; secret khi player vào phòng đánh dấu.
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
    private float _scanTimer;

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

    private void Update()
    {
        if (LevelCompleteUI.IsShowing || !DataManager.IsLevelScene(_sceneName))
            return;

        _scanTimer += Time.unscaledDeltaTime;
        if (_scanTimer < 0.5f)
            return;

        _scanTimer = 0f;
        CountTagged("Enemy");
        CountTagged("Boss");
    }

    private void ResetSession()
    {
        _trackedEnemyIds.Clear();
        _secretIds.Clear();
        _foundSecrets.Clear();
        _enemiesKilled = 0;
        _enemiesTotal = 0;
        _scanTimer = 0f;
    }

    private IEnumerator CensusWhenReady()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        CensusSecrets();
        CensusEnemies();
        _censusRoutine = null;
    }

    public static LevelCompleteStats CaptureSnapshot()
    {
        EnsureExists();
        var tracker = Instance;
        float time = DataManager.Instance != null ? DataManager.Instance.LevelTimeSeconds : 0f;

        int total = tracker != null ? Mathf.Max(tracker._enemiesTotal, tracker._enemiesKilled) : 0;
        int killed = tracker != null ? tracker._enemiesKilled : 0;
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

    /// <summary>Enemy đã nằm sẵn trong scene (không phải spawn từ room/spawner đã cộng planned).</summary>
    public static void BindExistingEnemy(GameObject enemy)
    {
        EnsureExists()?.BindEnemy(enemy, incrementTotal: true);
    }

    /// <summary>Enemy spawn từ wave/spawner — tổng đã cộng lúc census.</summary>
    public static void BindSpawnedEnemy(GameObject enemy)
    {
        EnsureExists()?.BindEnemy(enemy, incrementTotal: false);
    }

    public static void NotifyEnemyKilled(GameObject enemy)
    {
        if (Instance == null || enemy == null)
            return;
        if (LevelCompleteUI.IsShowing)
            return;

        // Tag có thể đã đổi thành Untagged lúc Die — vẫn tính kill.
        int id = enemy.transform.root.GetInstanceID();
        if (Instance._trackedEnemyIds.Add(id))
            Instance._enemiesTotal++;

        Instance._enemiesKilled++;
        if (Instance._enemiesKilled > Instance._enemiesTotal)
            Instance._enemiesTotal = Instance._enemiesKilled;
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
    }

    private void BindEnemy(GameObject enemy, bool incrementTotal)
    {
        if (enemy == null || !IsCountableEnemy(enemy))
            return;

        GameObject root = enemy.transform.root.gameObject;
        int id = root.GetInstanceID();
        if (!_trackedEnemyIds.Add(id))
            return;

        if (root.GetComponent<EnemyKillReporter>() == null)
            root.AddComponent<EnemyKillReporter>();

        if (incrementTotal)
            _enemiesTotal++;
    }

    private void CensusEnemies()
    {
        CountTagged("Enemy");
        CountTagged("Boss");

        var rooms = FindObjectsByType<RoomSpawnController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room == null)
                continue;

            int planned = room.CountPlannedEnemies();
            _enemiesTotal += planned;
            if (room.IsCleared)
                _enemiesKilled += planned;
        }

        var spawners = FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
        {
            var spawner = spawners[i];
            if (spawner == null || spawner.HasSpawned)
                continue;
            _enemiesTotal += spawner.CountPlannedEnemies();
        }
    }

    private void CountTagged(string tag)
    {
        GameObject[] found;
        try
        {
            found = GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
            return;
        }

        for (int i = 0; i < found.Length; i++)
            BindEnemy(found[i], incrementTotal: true);
    }

    private void CensusSecrets()
    {
        var markers = FindObjectsByType<SecretRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null)
                _secretIds.Add(markers[i].SecretId);
        }

        var rooms = FindObjectsByType<RoomCameraPriority>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = rooms[i];
            if (room != null && room.IsSecretRoom)
                _secretIds.Add(room.SecretId);
        }
    }

    private static bool IsCountableEnemy(GameObject go)
    {
        if (go == null)
            return false;

        Transform root = go.transform.root;
        if (CompareTagSafe(root.gameObject, "Player"))
            return false;
        if (root.GetComponent<HeartOfTheNight.Player.PlayerHealth>() != null)
            return false;

        return HasEnemyTag(go) || HasEnemyTag(root.gameObject);
    }

    private static bool HasEnemyTag(GameObject go)
    {
        return CompareTagSafe(go, "Enemy") || CompareTagSafe(go, "Boss");
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
