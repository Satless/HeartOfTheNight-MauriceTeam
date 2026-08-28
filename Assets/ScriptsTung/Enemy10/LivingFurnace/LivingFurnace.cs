using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HeartOfTheNight.Common;

public class LivingFurnaceImg : MonoBehaviour, IDamageable
{
    [Header("Sinh tồn & Hoạt ảnh")]
    public int maxHealth = 200;
    private int currentHealth;
    private bool isDead = false;
    public Animator anim;

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

    // 1. FIX LỖI RỚT MAP: Khai báo thêm Rigidbody2D
    private Rigidbody2D rb;

    private float idleSoundTimer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        myCol = GetComponent<Collider2D>();

        // 2. FIX LỖI RỚT MAP: Tìm Rigidbody2D lúc mới sinh ra
        rb = GetComponent<Rigidbody2D>();

        currentHealth = maxHealth;
        SetupXuyenThau();
    }

    void Update()
    {
        if (isDead) return;

        if (isSpawning) return;

        activeMinions.RemoveAll(minion => minion == null);

        if (player != null)
        {
            float distanceX = Mathf.Abs(player.position.x - transform.position.x);
            float distanceY = Mathf.Abs(player.position.y - transform.position.y);

            if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
            {
                if (activeMinions.Count == 0)
                {
                    StartCoroutine(SpawnWaveRoutine());
                }
            }

            else 
            {
                idleSoundTimer -= Time.fixedDeltaTime;
                if (idleSoundTimer <= 0f)
                {
                    //SoundManager.Instance.PlaySound3D("Player", "Slide", transform.position);
                    AudioEvents.TriggerSound3D("Player", "Slide", "n", transform.position);
                    idleSoundTimer = 2f; // Phát lại sau mỗi 0.2s
                }
            }
        }
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;

        yield return new WaitForSeconds(delayBetweenWaves);

        AudioEvents.TriggerSound3D("Enemy", "LivingFurnace", "Attack", transform.position);

        if (isDead) yield break;

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

        if (player != null)
        {
            Collider2D pCol = player.GetComponent<Collider2D>();
            if (pCol != null) Physics2D.IgnoreCollision(myCol, pCol, true);
        }

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

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        AudioEvents.TriggerSound3D("Enemy", "LivingFurnace", "Hurt", transform.position);

        Debug.Log("Lò ấp bị chém! Máu: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        AudioEvents.TriggerSound3D("Enemy", "LivingFurnace", "Die", transform.position);
        Debug.Log("Lò ấp đã bị phá hủy!");

        gameObject.tag = "Untagged";

        // 3. FIX LỖI RỚT MAP: Đóng băng trọng lực trước khi tắt va chạm
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Chặn gia tốc đang rơi dở (nếu có)
            rb.simulated = false; // Tắt hoàn toàn tác động vật lý (trọng lực)
        }

        if (myCol != null) myCol.enabled = false;

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }

        Destroy(gameObject, 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRadius * 2, 0.1f, 0));
    }
}