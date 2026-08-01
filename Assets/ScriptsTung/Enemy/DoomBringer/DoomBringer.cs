using UnityEngine;
using System.Collections;

public class DoomBringer : MonoBehaviour
{
    [Header("Hoạt ảnh (Animation)")]
    public Animator anim; // Chỉ giữ lại để nó tự chạy clip Idle của bạn

    [Header("Chỉ số Sinh tồn & Giai đoạn")]
    public int maxHealth = 1000;
    private int currentHealth;
    private bool isPhase2 = false;

    [Header("Buff Giai đoạn 2 (< 50% HP)")]
    public float phase2SpeedMulti = 1.5f;
    public float phase2FireRateMulti = 0.5f;

    [Header("Di chuyển")]
    public float moveSpeed = 3.5f;
    public bool isWallOfFleshMode = false;
    private float fixedDirection = 1f;

    [Header("Chu kỳ Trạng Thái (State Machine)")]
    public float timePerState = 5f;
    public float transitionDelay = 1f;
    private int currentState = 1;
    private float stateTimer;
    private bool isTransitioning = false;

    [Header("Vũ khí & Prefabs")]
    public Transform firePoint;

    [Space]
    public GameObject bombPrefab;
    public float bombFireRate = 1.5f;
    public float bombForce = 10f;

    [Space]
    public GameObject laserPrefab;
    public float laserFireRate = 0.5f;
    public float laserSpeed = 20f;

    [Space]
    public GameObject kamikazePrefab;
    private bool hasSummoned = false;

    private Transform player;
    private Rigidbody2D rb;
    private float attackTimer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();

        if (anim == null) anim = GetComponent<Animator>();

        currentHealth = maxHealth;
        stateTimer = timePerState;

        if (player != null)
        {
            fixedDirection = Mathf.Sign(player.position.x - transform.position.x);
        }
    }

    void Update()
    {
        if (player == null) return;

        MoveRelentlessly();

        if (!isTransitioning)
        {
            HandleStateSwitching();
            ExecuteCurrentState();
        }
    }

    // ================== HỆ THỐNG MÁU & GIAI ĐOẠN ==================

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Doom Bringer bị đánh! Máu: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= maxHealth / 2 && !isPhase2)
        {
            EnterPhase2();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void EnterPhase2()
    {
        isPhase2 = true;
        Debug.Log("DOOM BRINGER NỔI ĐIÊN! VÀO GIAI ĐOẠN 2!");

        moveSpeed *= phase2SpeedMulti;
        bombFireRate *= phase2FireRateMulti;
        laserFireRate *= phase2FireRateMulti;
        attackTimer *= phase2FireRateMulti;
        transitionDelay *= phase2FireRateMulti;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.red;
    }

    void Die()
    {
        Debug.Log("Boss Doom Bringer đã bị tiêu diệt!");
        Destroy(gameObject);
    }

    // ================== LOGIC HÀNH ĐỘNG ==================

    void MoveRelentlessly()
    {
        float dir = isWallOfFleshMode ? fixedDirection : Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    void HandleStateSwitching()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0)
        {
            StartCoroutine(TransitionRoutine());
        }
    }

    IEnumerator TransitionRoutine()
    {
        isTransitioning = true;
        Debug.Log("Boss ngừng bắn, đang nghỉ " + transitionDelay + " giây...");

        yield return new WaitForSeconds(transitionDelay);

        currentState++;
        if (currentState > 3) currentState = 1;

        stateTimer = timePerState;
        hasSummoned = false;
        attackTimer = 0f;

        isTransitioning = false;
        Debug.Log("Chuyển sang Trạng Thái: " + currentState);
    }

    void ExecuteCurrentState()
    {
        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case 1: // NÃ BOM
                if (attackTimer <= 0)
                {
                    ShootBomb();
                    attackTimer = bombFireRate;
                }
                break;

            case 2: // BẮN LAZE
                if (attackTimer <= 0)
                {
                    ShootLaser();
                    attackTimer = laserFireRate;
                }
                break;

            case 3: // TRIỆU HỒI KAMIKAZE
                if (!hasSummoned)
                {
                    StartCoroutine(SummonKamikazesRoutine());
                    hasSummoned = true;
                }
                break;
        }
    }

    void ShootBomb()
    {
        if (bombPrefab == null || firePoint == null) return;

        GameObject bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();

        if (bombRb != null)
        {
            Vector2 direction = (player.position - firePoint.position).normalized;
            direction.y += 0.5f;
            bombRb.linearVelocity = direction * bombForce;
        }
    }

    void ShootLaser()
    {
        if (laserPrefab == null || firePoint == null) return;

        GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D laserRb = laser.GetComponent<Rigidbody2D>();

        if (laserRb != null)
        {
            Vector2 direction = (player.position - firePoint.position).normalized;
            laserRb.linearVelocity = direction * laserSpeed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laser.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    IEnumerator SummonKamikazesRoutine()
    {
        if (kamikazePrefab == null) yield break;

        int soLuongDe = isPhase2 ? 5 : 3;

        for (int i = 0; i < soLuongDe; i++)
        {
            Vector2 spawnPos = new Vector2(transform.position.x, transform.position.y + 1.5f + (i * 0.5f));
            Instantiate(kamikazePrefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(0.3f);
        }
    }
}