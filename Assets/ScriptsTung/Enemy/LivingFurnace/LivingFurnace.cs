using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LivingFurnace : MonoBehaviour
{
    [Header("Cài đặt Triệu hồi (Spawner)")]
    public GameObject burningCorpsePrefab;

    public int maxMinions = 4;
    public float spawnRadius = 2f;
    public float delayBetweenWaves = 2f;

    [Header("Tầm phát hiện Player")]
    public float detectionRangeX = 12f;
    public float detectionRangeY = 5f;

    private List<GameObject> activeMinions = new List<GameObject>();
    private bool isSpawning = false;

    private Transform player;
    private Collider2D myCol;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        myCol = GetComponent<Collider2D>();
        SetupXuyenThau();
    }

    void Update()
    {
        if (isSpawning) return;

        // Dọn dẹp danh sách quái đã chết
        activeMinions.RemoveAll(minion => minion == null);

        if (player != null)
        {
            // Đo khoảng cách giữa Lò ấp và Player
            float distanceX = Mathf.Abs(player.position.x - transform.position.x);
            float distanceY = Mathf.Abs(player.position.y - transform.position.y);

            // NẾU PLAYER BƯỚC VÀO VÙNG PHÁT HIỆN
            if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
            {
                // VÀ nếu trên sân không còn con quái nào
                if (activeMinions.Count == 0)
                {
                    // Thì mới bắt đầu đẻ quái
                    StartCoroutine(SpawnWaveRoutine());
                }
            }
        }
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        yield return new WaitForSeconds(delayBetweenWaves);

        float pivotOffset = 0f;
        Collider2D prefabCol = burningCorpsePrefab.GetComponent<Collider2D>();
        if (prefabCol != null)
        {
            pivotOffset = burningCorpsePrefab.transform.position.y - prefabCol.bounds.min.y;
        }

        for (int i = 0; i < maxMinions; i++)
        {
            float randomX = transform.position.x + Random.Range(-spawnRadius, spawnRadius);
            Vector2 rayStart = new Vector2(randomX, transform.position.y + 3f);
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 10f);

            Vector2 spawnPos = new Vector2(randomX, transform.position.y);

            if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            {
                spawnPos = new Vector2(randomX, hit.point.y + pivotOffset + 0.05f);
            }

            GameObject newMinion = Instantiate(burningCorpsePrefab, spawnPos, Quaternion.identity);
            activeMinions.Add(newMinion);

            yield return new WaitForSeconds(0.3f);
        }

        isSpawning = false;
    }

    void SetupXuyenThau()
    {
        if (myCol == null) return;

        // 1. Xuyên Player
        if (player != null)
        {
            Collider2D pCol = player.GetComponent<Collider2D>();
            if (pCol != null) Physics2D.IgnoreCollision(myCol, pCol, true);
        }

        // 2. Xuyên tất cả quái vật khác có Tag là "Enemy"
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in allEnemies)
        {
            Collider2D enemyCol = enemyObj.GetComponent<Collider2D>();
            if (enemyCol != null && enemyCol != myCol)
            {
                Physics2D.IgnoreCollision(myCol, enemyCol, true);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vùng phát hiện Player (HÌNH CHỮ NHẬT MÀU CAM)
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        // Vẽ phạm vi đẻ quái (ĐƯỜNG KẺ MÀU VÀNG)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRadius * 2, 0.1f, 0));
    }
}