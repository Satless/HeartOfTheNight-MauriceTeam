using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Các loại quái (Kéo Prefab vào đây)")]
    public GameObject[] enemyPrefabs;

    [Header("Các vị trí xuất hiện (Kéo Object vào đây)")]
    public Transform[] spawnPoints;

    private bool hasSpawned = false; // Biến đánh dấu để chỉ gọi quái 1 lần

    public bool HasSpawned => hasSpawned;

    public int CountPlannedEnemies()
    {
        return spawnPoints != null ? spawnPoints.Length : 0;
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
        foreach (Transform point in spawnPoints)
        {
            // Trừ khi bạn quên không bỏ quái vào danh sách
            if (enemyPrefabs.Length > 0)
            {
                // Chọn ngẫu nhiên 1 loại quái (Big Corpse hoặc Burning Corpse)
                int randomIndex = Random.Range(0, enemyPrefabs.Length);
                GameObject quaiMuonGoi = enemyPrefabs[randomIndex];

                // Lệnh Instantiate dùng để đẻ (clone) quái ra màn hình ngay tại vị trí của point
                GameObject spawned = Instantiate(quaiMuonGoi, point.position, Quaternion.identity);
                LevelStatsTracker.BindSpawnedEnemy(spawned);
            }
        }

        Debug.Log("Player dẫm bẫy! Đã gọi hội quái ra!");
    }
}
////da