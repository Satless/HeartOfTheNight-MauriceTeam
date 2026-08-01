using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LivingFurnaceImg : MonoBehaviour
{
    [Header("Sinh tồn & Hoạt ảnh")]
    public int maxHealth = 200;
    private int currentHealth;/////
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

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        myCol = GetComponent<Collider2D>();

        if (anim == null) anim = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;

        SetupXuyenThau();
    }

    void Update()
    {
        if (isSpawning || isDead) return;

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
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log("Living Furnace nhận " + damage + " sát thương! Máu: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Living Furnace đã bị tiêu diệt!");

        StopAllCoroutines();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Khóa vật lý y hệt DoomBringer
        }

        gameObject.tag = "Untagged";
        if (myCol != null) myCol.enabled = false;

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }

        Destroy(gameObject, 0.5f);
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;
        yield return new WaitForSeconds(delayBetweenWaves);

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRadius * 2, 0.1f, 0));
    }
}
