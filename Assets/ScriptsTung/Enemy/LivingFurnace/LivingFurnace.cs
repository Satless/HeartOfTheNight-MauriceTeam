using UnityEngine;
using System.Collections.Generic;

public class LivingFurnace : MonoBehaviour
{
    [Header("Tầm nhìn (Quét Ngang & Dọc)")]
    public float detectionRangeX = 15f;  // Khoảng cách phát hiện theo chiều ngang
    public float detectionRangeY = 5f;   // Giới hạn chiều cao

    [Header("Cài đặt Triệu hồi")]
    public GameObject burningCorpsePrefab;
    public int maxSpawns = 4;
    public float spawnRadius = 1.5f;
    public float spawnDelay = 1f; // Chờ 1 giây sau khi phát hiện mới đẻ quái

    private List<GameObject> activeMinions = new List<GameObject>();
    private Transform player;
    private float spawnTimer = 0f;

    void Start()
    {
        // Xóa lệnh gọi SpawnBatch() ở đây đi để đầu game nó không tự đẻ nữa
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;

        // 1. Dọn dẹp danh sách quái chết
        activeMinions.RemoveAll(minion => minion == null);

        // 2. Tính khoảng cách giữa lò và người chơi
        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        // NẾU PLAYER ĐI VÀO VÙNG PHÁT HIỆN
        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            // Và nếu đợt quái cũ đã chết sạch (hoặc chưa đẻ đợt nào)
            if (activeMinions.Count == 0)
            {
                // Bắt đầu đếm ngược thời gian khởi động lò
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= spawnDelay)
                {
                    SpawnBatch();
                    spawnTimer = 0f; // Reset đồng hồ sau khi đẻ xong
                }
            }
        }
        else
        {
            // Nếu Player chạy ra khỏi vùng phát hiện -> Tắt lò, reset đồng hồ
            spawnTimer = 0f;
        }
    }

    void SpawnBatch()
    {
        if (burningCorpsePrefab == null)
        {
            Debug.LogWarning("Bạn chưa kéo Prefab Burning Corpse vào Living Furnace!");
            return;
        }

        for (int i = 0; i < maxSpawns; i++)
        {
            // Chọn vị trí rớt ngẫu nhiên
            float randomX = Random.Range(-spawnRadius, spawnRadius);
            Vector2 spawnPosition = new Vector2(transform.position.x + randomX, transform.position.y);

            GameObject newMinion = Instantiate(burningCorpsePrefab, spawnPosition, Quaternion.identity);
            activeMinions.Add(newMinion);
        }

        Debug.Log("Living Furnace đã phát hiện Player và triệu hồi " + maxSpawns + " con quái!");
    }

    // ================= VẼ KHUNG TRONG EDITOR =================
    void OnDrawGizmosSelected()
    {
        // Vẽ Tầm Phát Hiện (Khung vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        // Vẽ Vùng Rớt Quái (Vòng tròn màu cam)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}