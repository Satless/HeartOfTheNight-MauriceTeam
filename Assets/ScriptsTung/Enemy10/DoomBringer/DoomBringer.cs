using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common;
using UnityEngine.UI;
using TMPro;

public class DoomBringer : MonoBehaviour, IDamageable
{
    [Header("Hoạt ảnh (Animation)")]
    public Animator anim;

    [Header("Giao Diện (UI Boss)")]
    public GameObject bossUIContainer;
    public Image healthFillImage;
    public TextMeshProUGUI nameText;
    public string bossName = "DOOM BRINGER";

    [Header("Cài đặt Chết (Theo Animation)")]
    [Tooltip("Thời gian chạy animation NearlyDead (Ví dụ: 2 giây)")]
    public float nearlyDeadDuration = 2f;
    [Tooltip("Thời gian chạy clip Dead trước khi Boss biến mất (Ví dụ: 0.5 giây)")]
    public float deadDuration = 0.5f;

    [Space]
    [Tooltip("Nếu bác muốn lúc nổ (Dead) nó bự lên thì giữ số này, không thì chỉnh về 1")]
    public float explosionScale = 5f;
    public float explosionYOffset = 1.5f;
    public float deathShakeAmplitude = 0.5f;

    [Header("Chỉ số Sinh tồn & Giai đoạn")]
    public int maxHealth = 1000;
    private int currentHealth;
    public bool isDead = false;
    private bool isPhase2 = false;

    [Header("Kiểm tra Mặt đất (Dùng Layer)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public bool isGrounded;

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
    public float bombFlightTime = 1.2f;

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

        if (anim == null) anim = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        stateTimer = timePerState;

        if (player != null)
        {
            fixedDirection = Mathf.Sign(player.position.x - transform.position.x);
        }

        if (nameText != null) nameText.text = bossName;
        if (healthFillImage != null) healthFillImage.fillAmount = 1f;
        if (bossUIContainer != null) bossUIContainer.SetActive(true);
    }

    void Update()
    {
        if (player == null || isDead) return;

        CheckGroundStatus();

        if (!isGrounded)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        MoveRelentlessly();

        if (!isTransitioning)
        {
            HandleStateSwitching();
            ExecuteCurrentState();
        }
    }

    void CheckGroundStatus()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = (float)currentHealth / maxHealth;
        }

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
        moveSpeed *= phase2SpeedMulti;
        bombFireRate *= phase2FireRateMulti;
        laserFireRate *= phase2FireRateMulti;
        attackTimer *= phase2FireRateMulti;
        transitionDelay *= phase2FireRateMulti;
        bombFlightTime *= 0.8f;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.red;
    }

    void Die()
    {
        isDead = true;

        if (bossUIContainer != null) bossUIContainer.SetActive(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        gameObject.tag = "Untagged";
        Collider2D[] cols = GetComponents<Collider2D>();
        foreach (Collider2D c in cols) c.enabled = false;

        StartCoroutine(DeathSequenceRoutine());
    }

    // 🔥 HÀM MỚI: XỬ LÝ CHẾT THEO ĐÚNG THỨ TỰ ANIMATION BÁC MUỐN
    IEnumerator DeathSequenceRoutine()
    {
        // 🔥 FIX: Trả lại màu trắng nguyên bản cho ảnh để không bị ám đỏ lúc chết
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        // 1. Kích hoạt Animation NearlyDead
        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("NearlyDead");
        }

        // Tùy chọn: Rung màn hình trong lúc nó đang giãy (NearlyDead)
        try { CameraShake.Shake(deathShakeAmplitude, nearlyDeadDuration); } catch { }

        // Chờ cho animation NearlyDead chạy xong
        yield return new WaitForSeconds(nearlyDeadDuration);

        // 2. Chuẩn bị chạy Animation Dead (Nổ)
        float signX = Mathf.Sign(transform.localScale.x);
        transform.localScale = new Vector3(signX * explosionScale, explosionScale, 1f);
        transform.position = new Vector3(transform.position.x, transform.position.y + explosionYOffset, transform.position.z);

        if (anim != null)
        {
            anim.SetTrigger("Dead");
        }

        // Tùy chọn: Rung màn hình cú chót cực mạnh lúc Nổ
        try { CameraShake.Shake(deathShakeAmplitude * 1.5f, 0.5f); } catch { }

        // 3. Xóa Boss hoàn toàn khỏi game
        Destroy(gameObject, deadDuration);
    }

    // --- Các hàm di chuyển & tấn công ---
    void MoveRelentlessly()
    {
        float dir = isWallOfFleshMode ? fixedDirection : Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
        transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    void HandleStateSwitching()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0) StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(transitionDelay);

        currentState++;
        if (currentState > 3) currentState = 1;

        stateTimer = timePerState;
        hasSummoned = false;
        attackTimer = 0f;
        isTransitioning = false;
    }

    void ExecuteCurrentState()
    {
        attackTimer -= Time.deltaTime;
        switch (currentState)
        {
            case 1:
                if (attackTimer <= 0) { ShootBomb(); attackTimer = bombFireRate; }
                break;
            case 2:
                if (attackTimer <= 0) { ShootLaser(); attackTimer = laserFireRate; }
                break;
            case 3:
                if (!hasSummoned) { StartCoroutine(SummonKamikazesRoutine()); hasSummoned = true; }
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
            Vector2 targetPos = new Vector2(player.position.x, player.position.y + 0.5f);
            Vector2 distance = targetPos - (Vector2)firePoint.position;
            float gravity = Mathf.Abs(Physics2D.gravity.y * bombRb.gravityScale);
            float velocityX = distance.x / bombFlightTime;
            float velocityY = (distance.y / bombFlightTime) + (0.5f * gravity * bombFlightTime);
            bombRb.linearVelocity = new Vector2(velocityX, velocityY);
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

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}