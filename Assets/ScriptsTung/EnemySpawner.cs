using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Các loại quái (Kéo Prefab vào đây)")]
    public GameObject[] enemyPrefabs;

    [Header("Các vị trí xuất hiện (Kéo Object vào đây)")]
    public Transform[] spawnPoints;

    private bool hasSpawned = false; // Biến đánh dấu để chỉ gọi quái 1 lần
    private readonly List<GameObject> spawnedEnemies = new();

    public bool HasSpawned => hasSpawned;

    public int CountPlannedEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints == null)
            return 0;

        int count = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
                count++;
        }
        return count;
    }

    public int CountDefeatedEnemies()
    {
        int planned = CountPlannedEnemies();
        if (planned <= 0 || !hasSpawned)
            return 0;

        int dead = 0;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] == null)
                dead++;
        }

        return Mathf.Clamp(dead, 0, planned);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem ai vừa chạm vào. Nếu là Player và vùng này chưa từng sinh quái
        if (collision.CompareTag("Player") && !hasSpawned)
        {
            hasSpawned = true; // Đánh dấu là đã sinh quái rồi (để player đi qua đi lại không bị spam)
            SpawnEnemies();
        }
    }

    void SpawnEnemies()
    {
        // Duyệt qua từng vị trí trong danh sách Spawn Points
        if (spawnPoints == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            int randomIndex = Random.Range(0, enemyPrefabs.Length);
            GameObject quaiMuonGoi = enemyPrefabs[randomIndex];
            if (quaiMuonGoi == null)
                continue;

            GameObject spawned = Instantiate(quaiMuonGoi, point.position, Quaternion.identity);
            spawnedEnemies.Add(spawned);
            LevelStatsTracker.BindSpawnedEnemy(spawned);
        }

        Debug.Log("Player dẫm bẫy! Đã gọi hội quái ra!");
        AudioEvents.TriggerSound3D("Enemy", "General", "Spawn", transform.position);
    }
}
////da