using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LivingFurnace : MonoBehaviour
{
    [Header("Cài đặt Triệu hồi (Spawner)")]
    [Tooltip("Kéo Prefab con Burning Corpse vào đây")]
    public GameObject burningCorpsePrefab;

    public int maxMinions = 4;             // Chỉ tối đa 4 con
    public float spawnRadius = 2f;         // Khoảng cách đẻ quái xung quanh lò
    public float delayBetweenWaves = 2f;   // Thời gian nghỉ trước khi đẻ đợt mới (để Player thở)

    // Danh sách dùng để theo dõi 4 con quái lửa
    private List<GameObject> activeMinions = new List<GameObject>();
    private bool isSpawning = false;

    void Start()
    {
        // Vừa vào game là đẻ luôn 4 con đợt đầu tiên
        StartCoroutine(SpawnWaveRoutine());
    }

    void Update()
    {
        if (isSpawning) return;

        // BƯỚC QUAN TRỌNG: 
        // Khi tụi Burning Corpse bị chém chết (bị Destroy), chúng sẽ biến thành "null" trong game.
        // Lệnh này sẽ quét danh sách và xóa hết những con "null" đó đi.
        activeMinions.RemoveAll(minion => minion == null);

        // KIỂM TRA ĐIỀU KIỆN: Nếu 4 con đều đã chết (danh sách bị xóa sạch về 0)
        if (activeMinions.Count == 0)
        {
            // Thì gọi hàm sinh ra 4 con mới
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;

        // Chờ một chút trước khi đẻ để game không bị quá dồn dập
        yield return new WaitForSeconds(delayBetweenWaves);

        for (int i = 0; i < maxMinions; i++)
        {
            // Tìm một vị trí ngẫu nhiên xung quanh cái Lò
            Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

            // Ép quái rớt xuống đất nếu nó bay lơ lửng trên không (Dùng Raycast bắn xuống đất)
            RaycastHit2D hit = Physics2D.Raycast(randomPos, Vector2.down, 5f);
            if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            {
                randomPos.y = hit.point.y + 0.5f; // Nâng lên 1 tí cho khỏi kẹt sàn
            }

            // Sinh ra quái và lập tức nạp nó vào Danh Sách để theo dõi mạng sống
            GameObject newMinion = Instantiate(burningCorpsePrefab, randomPos, Quaternion.identity);
            activeMinions.Add(newMinion);

            // Khựng lại 0.3s cho mỗi con đẻ ra nhìn cho mượt, tránh đẻ 1 cục đè lên nhau
            yield return new WaitForSeconds(0.3f);
        }

        isSpawning = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn màu vàng để bạn dễ hình dung khu vực quái sẽ rớt ra
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}