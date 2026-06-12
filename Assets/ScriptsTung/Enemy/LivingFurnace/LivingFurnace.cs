using UnityEngine;
using System.Collections.Generic;

public class LivingFurnace : MonoBehaviour
{
    [Header("Tầm phát hiện (Chỉ nhả quái khi Player ở gần)")]
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;

    [Header("Cài đặt Triệu hồi")]
    public GameObject burningCorpsePrefab;
    public int maxSpawns = 4;
    public float spawnRadius = 1.5f;

    private Transform player;
    private List<GameObject> activeMinions = new List<GameObject>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        // Dọn dẹp danh sách quái chết
        activeMinions.RemoveAll(minion => minion == null);

        // Tính khoảng cách
        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        // NẾU PLAYER LỌT VÀO VÙNG NHÌN THẤY VÀ HẾT QUÁI -> MỚI TRIỆU HỒI
        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (activeMinions.Count == 0)
            {
                SpawnBatch();
            }
        }
    }

    void SpawnBatch()
    {
        if (burningCorpsePrefab == null) return;

        for (int i = 0; i < maxSpawns; i++)
        {
            float randomX = Random.Range(-spawnRadius, spawnRadius);
            Vector2 spawnPosition = new Vector2(transform.position.x + randomX, transform.position.y);
            GameObject newMinion = Instantiate(burningCorpsePrefab, spawnPosition, Quaternion.identity);
            activeMinions.Add(newMinion);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f); // Màu Cam 
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));
    }
}