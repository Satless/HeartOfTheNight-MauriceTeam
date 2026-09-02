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
    public float nearlyDeadDuration = 2f;
    public float deadDuration = 0.5f;
    [Space]
    public float explosionScale = 5f;
    public float explosionYOffset = 1.5f;
    public float deathShakeAmplitude = 0.5f;

    [Header("Chỉ số Sinh tồn & Giai đoạn")]
    public int maxHealth = 1000;
    private int currentHealth;
    public bool isDead = false;
    private bool isPhase2 = false;

    [Header("Buff Giai đoạn 2 (< 50% HP)")]
    public float phase2SpeedMulti = 1.5f;
    public float phase2FireRateMulti = 0.5f;

    [Header("Di chuyển")]
    public float moveSpeed = 3.5f;

    public bool isWallOfFleshMode = true;
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

    [Header("Chống Kẹt Spawn Cực Mạnh")]
    [Tooltip("BẮT BUỘC PHẢI KÉO LAYER GROUND/MAP VÀO ĐÂY NHÉ BÁC!")]
    public LayerMask obstacleLayer;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float attackTimer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }

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

        if (!isTransitioning)
        {
            HandleStateSwitching();
            ExecuteCurrentState();
        }
    }

    void FixedUpdate()
    {
        if (player == null || isDead) return;

        MoveRelentlessly();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (healthFillImage != null) healthFillImage.fillAmount = (float)currentHealth / maxHealth;

        if (currentHealth <= maxHealth / 2 && !isPhase2) EnterPhase2();
        if (currentHealth <= 0) Die();
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

    IEnumerator DeathSequenceRoutine()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        if (anim != null) { anim.enabled = true; anim.SetTrigger("NearlyDead"); }
        try { CameraShake.Shake(deathShakeAmplitude, nearlyDeadDuration); } catch { }

        yield return new WaitForSeconds(nearlyDeadDuration);

        float signX = Mathf.Sign(transform.localScale.x);
        transform.localScale = new Vector3(signX * explosionScale, explosionScale, 1f);
        transform.position = new Vector3(transform.position.x, transform.position.y + explosionYOffset, transform.position.z);

        if (anim != null) anim.SetTrigger("Dead");
        try { CameraShake.Shake(deathShakeAmplitude * 1.5f, 0.5f); } catch { }

        Destroy(gameObject, deadDuration);
    }

    void MoveRelentlessly()
    {
        float dir = isWallOfFleshMode ? fixedDirection : Mathf.Sign(player.position.x - transform.position.x);

        Vector2 newPosition = rb.position + new Vector2(dir * moveSpeed, 0f) * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

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
            case 1: if (attackTimer <= 0) { ShootBomb(); attackTimer = bombFireRate; } break;
            case 2: if (attackTimer <= 0) { ShootLaser(); attackTimer = laserFireRate; } break;
            case 3: if (!hasSummoned) { StartCoroutine(SummonKamikazesRoutine()); hasSummoned = true; } break;
        }
    }

    // 🔥 TIA QUÉT NGƯỢC (REVERSE RAYCAST)
    Vector2 GetSafeSpawnPosition(Vector2 intendedPos)
    {
        if (player == null) return intendedPos;

        Vector2 playerPos = new Vector2(player.position.x, player.position.y + 0.5f); // Nhắm vào giữa ngực Player
        Vector2 dirToTarget = (intendedPos - playerPos).normalized;
        float distance = Vector2.Distance(playerPos, intendedPos);

        // Bắn tia từ Player ngược về phía điểm định đẻ
        RaycastHit2D hit = Physics2D.Raycast(playerPos, dirToTarget, distance, obstacleLayer);

        if (hit.collider != null)
        {
            // Tia đụng trúng tường che chắn! Dời điểm đẻ ra sát MẶT NGOÀI bức tường
            return hit.point - (dirToTarget * 0.4f);
        }

        return intendedPos;
    }

    void ShootBomb()
    {
        if (bombPrefab == null || firePoint == null) return;

        Vector2 safePos = GetSafeSpawnPosition(firePoint.position);
        GameObject bomb = Instantiate(bombPrefab, safePos, Quaternion.identity);

        Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();
        if (bombRb != null)
        {
            Vector2 targetPos = new Vector2(player.position.x, player.position.y + 0.5f);
            Vector2 distance = targetPos - safePos;
            float gravity = Mathf.Abs(Physics2D.gravity.y * bombRb.gravityScale);
            float velocityX = distance.x / bombFlightTime;
            float velocityY = (distance.y / bombFlightTime) + (0.5f * gravity * bombFlightTime);
            bombRb.linearVelocity = new Vector2(velocityX, velocityY);
        }
    }

    void ShootLaser()
    {
        if (laserPrefab == null || firePoint == null) return;

        Vector2 safePos = GetSafeSpawnPosition(firePoint.position);
        GameObject laser = Instantiate(laserPrefab, safePos, Quaternion.identity);

        Rigidbody2D laserRb = laser.GetComponent<Rigidbody2D>();
        if (laserRb != null)
        {
            Vector2 direction = ((Vector2)player.position - safePos).normalized;
            laserRb.linearVelocity = direction * laserSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laser.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    IEnumerator SummonKamikazesRoutine()
    {
        if (kamikazePrefab == null || firePoint == null) yield break;
        int soLuongDe = isPhase2 ? 5 : 3;
        for (int i = 0; i < soLuongDe; i++)
        {
            // 🔥 FIX MAP HẸP: Không cộng thêm trục Y nữa!
            // Đẻ tất cả quái ngay tại 1 tọa độ an toàn, tụi nó sẽ tự tách nhau ra
            Vector2 safePos = GetSafeSpawnPosition(firePoint.position);

            GameObject kami = Instantiate(kamikazePrefab, safePos, Quaternion.identity);

            KamikazeEnemy kamiScript = kami.GetComponent<KamikazeEnemy>();
            if (kamiScript != null)
            {
                kamiScript.ActivateBossMode();
            }

            // Giãn thời gian đẻ ra một tí để con trước bay đi chỗ khác, nhường chỗ cho con sau
            yield return new WaitForSeconds(0.6f);
        }
    }
}