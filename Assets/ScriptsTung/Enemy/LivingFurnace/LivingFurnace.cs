using UnityEngine;
using System.Collections.Generic; // Cần thư viện này để dùng List (Danh sách)

public class LivingFurnace : MonoBehaviour
{
    [Header("Cài đặt Triệu hồi")]
    public GameObject burningCorpsePrefab; // Kéo Prefab của quái lửa vào đây
    public int maxSpawns = 4; // Số lượng đệ tử mỗi đợt
    public float spawnRadius = 1.5f; // Sinh ngẫu nhiên quanh cái lò 1.5 unit để không đè vào nhau

    // Danh sách lưu trữ các con quái đang sống
    private List<GameObject> activeMinions = new List<GameObject>();

    void Start()
    {
        // Khi cái lò vừa xuất hiện, triệu hồi ngay đợt đầu tiên
        SpawnBatch();
    }

    void Update()
    {
        // 1. Dọn dẹp danh sách: Quét qua xem có con nào bị Player chém chết (bị Destroy biến thành null) thì gạch tên nó đi
        activeMinions.RemoveAll(minion => minion == null);

        // 2. Kích hoạt đợt mới: Nếu danh sách rỗng (tức là cả 4 con đều đã chết)
        if (activeMinions.Count == 0)
        {
            SpawnBatch();
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
            // Chọn một vị trí X ngẫu nhiên xung quanh cái lò để quái rơi ra đỡ bị tụm lại 1 cục
            float randomX = Random.Range(-spawnRadius, spawnRadius);
            Vector2 spawnPosition = new Vector2(transform.position.x + randomX, transform.position.y);

            // Sinh quái ra màn hình
            GameObject newMinion = Instantiate(burningCorpsePrefab, spawnPosition, Quaternion.identity);

            // Thêm con quái vừa sinh vào danh sách để quản lý
            activeMinions.Add(newMinion);
        }

        Debug.Log("Living Furnace đã triệu hồi 4 con Burning Corpse!");
    }
}